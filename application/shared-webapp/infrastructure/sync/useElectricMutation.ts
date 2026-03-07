import { createTransaction } from "@tanstack/db";
import { type UseMutationResult, useMutation } from "@tanstack/react-query";
import { useMemo } from "react";
import type { HttpError } from "../http/errorHandler";
import { getLastElectricOffset } from "../http/queryClient";

interface ElectricMutationOptions<Tdata, Tvariables> {
  /** The async function that calls the API. Must return the response data. */
  mutationFn: (variables: Tvariables) => Promise<Tdata>;

  /**
   * Synchronous callback for optimistic updates (update/delete patterns).
   * Runs inside a TanStack DB transaction -- if mutationFn fails, changes roll back automatically.
   */
  onMutate?: (variables: Tvariables) => void;

  /**
   * Post-API insert callback (insert pattern).
   * Called after mutationFn succeeds. Use this instead of onMutate for inserts
   * since the server-generated data (ID, timestamps) is needed for the collection row.
   */
  onInsert?: (data: Tdata, variables: Tvariables) => void;

  /** Called after mutation succeeds and Electric sync is confirmed. */
  onSuccess?: (data: Tdata, variables: Tvariables) => void;

  /** Called on mutation failure. Transaction auto-rolls back if onMutate was used. */
  onError?: (error: HttpError, variables: Tvariables) => void;

  /** Collection utils with awaitTxId for Electric sync confirmation. Pass collection.utils. */
  // biome-ignore lint/suspicious/noExplicitAny: Collection utils type is Record<string, any> at runtime
  utils?: Record<string, any>;
}

type ElectricMutationResult<Tdata, Tvariables> = UseMutationResult<Tdata, HttpError, Tvariables> & {
  validationErrors: Record<string, string[]> | undefined;
};

/**
 * Hook that bridges TanStack Query mutations with TanStack DB transactions and Electric SQL sync.
 *
 * Three mutation paths:
 * 1. onMutate provided (updates/deletes): createTransaction for optimistic state with auto-rollback
 * 2. onInsert provided (inserts): API call first, then safe insert after success
 * 3. Neither: plain API call + awaitTxId
 */
export function useElectricMutation<Tdata = unknown, Tvariables = void>(
  options: ElectricMutationOptions<Tdata, Tvariables>
): ElectricMutationResult<Tdata, Tvariables> {
  const mutation = useMutation<Tdata, HttpError, Tvariables>({
    meta: { skipQueryInvalidation: true },
    mutationFn: async (variables: Tvariables) => {
      // Await Electric sync inside the transaction's mutationFn so the transaction stays
      // in "persisting" state, keeping optimistic state visible until the real data arrives.
      // The awaitTxId timeout is caught so the transaction always completes successfully.
      // We do NOT await isPersisted -- the TanStack Query mutation returns immediately after
      // the API call succeeds, while the transaction keeps optimistic state alive in the background.
      const awaitSyncConfirmation = async (offset: number) => {
        if (options.utils) {
          await options.utils.awaitTxId(offset).catch(() => {
            // Timeout is non-fatal: the API call succeeded and Electric will sync eventually
          });
        }
      };

      // Path 1: Optimistic update/delete with transaction (auto-rollback on failure)
      if (options.onMutate) {
        let capturedData: Tdata | undefined;

        const tx = createTransaction({
          autoCommit: false,
          mutationFn: async () => {
            capturedData = await options.mutationFn(variables);
            // Capture offset immediately after API call, before any collection handler can consume it
            const offset = getLastElectricOffset();
            if (offset != null) {
              await awaitSyncConfirmation(offset);
            }
          }
        });

        tx.mutate(() => {
          options.onMutate?.(variables);
        });

        // Start committing (awaits Electric sync in background) but don't block the mutation.
        // The transaction stays in "persisting" state, keeping optimistic state visible.
        // If the API call fails, we await to propagate the error and trigger rollback.
        const commitPromise = tx.commit();
        try {
          capturedData = await Promise.race([
            tx.isPersisted.promise.then(() => capturedData),
            // Wait just long enough for the API call to complete and capture data.
            // The mutationFn sets capturedData before awaiting sync, so once the API
            // call resolves, capturedData is available even if sync is still pending.
            new Promise<Tdata | undefined>((resolve) => {
              const check = () => {
                if (capturedData !== undefined) {
                  resolve(capturedData);
                } else {
                  // API call hasn't completed yet -- keep waiting
                  setTimeout(check, 10);
                }
              };
              check();
            })
          ]);
        } catch {
          // If isPersisted rejects (rollback), await commit to propagate the API error
          await commitPromise;
        }
        // biome-ignore lint/style/noNonNullAssertion: capturedData is set after API call completes
        return capturedData!;
      }

      // Path 2 & 3: Call API first
      const data = await options.mutationFn(variables);

      // Capture offset immediately after API call, before onInsert's collection ops can consume it
      const offset = getLastElectricOffset();

      // Path 2: Post-API insert -- wrap in a transaction so optimistic state persists
      // until Electric sync delivers the real data
      if (options.onInsert) {
        const tx = createTransaction({
          autoCommit: false,
          mutationFn: async () => {
            if (offset != null) {
              await awaitSyncConfirmation(offset);
            }
          }
        });

        tx.mutate(() => {
          options.onInsert?.(data, variables);
        });

        // Start committing in background -- don't block the mutation.
        // The transaction keeps optimistic state visible while awaiting Electric sync.
        tx.commit().catch(() => {});

        return data;
      }

      // Path 3: Plain mutation -- fire-and-forget Electric sync
      if (offset != null) {
        awaitSyncConfirmation(offset);
      }

      return data;
    },
    onSuccess: (data, variables) => {
      options.onSuccess?.(data, variables);
    },
    onError: (error, variables) => {
      options.onError?.(error, variables);
      // Replicate defaultOptions.mutations.onError: trigger global unhandledrejection handler
      // for non-validation errors so error toasts are shown automatically
      if (error.kind !== "validation") {
        Promise.reject(error);
      }
    }
  });

  const validationErrors = useMemo(
    () => (mutation.error?.kind === "validation" ? mutation.error.errors : undefined),
    [mutation.error]
  );

  return useMemo(() => ({ ...mutation, validationErrors }), [mutation, validationErrors]);
}
