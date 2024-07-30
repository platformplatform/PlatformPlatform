import type { ClientMethod, MaybeOptionalInit, ParseAsResponse } from "openapi-fetch";
import type {
  HttpMethod,
  MediaType,
  PathsWithMethod,
  ResponseObjectMap,
  SuccessResponse
} from "openapi-typescript-helpers";
import { createClientMethodWithProblemDetails, ProblemDetailsError } from "./ClientMethodWithProblemDetails";
import { useCallback, useEffect, useRef, useState } from "react";
import type { ProblemDetails } from "./ProblemDetails";

type UseApiReturnType<Data> = {
  loading: boolean;
  success: boolean | null;
  data?: Data;
  error?: ProblemDetails;
  refresh: () => void;
};

export function createApiReactHook<
  // biome-ignore lint/complexity/noBannedTypes: This is the exact type used in "openapi-typescript"
  Paths extends Record<string, Record<HttpMethod, {}>>,
  Method extends HttpMethod,
  Media extends MediaType = MediaType
>(clientMethod: ClientMethod<Paths, Method, Media>) {
  const apiMethod = createClientMethodWithProblemDetails(clientMethod);
  return <
    Path extends PathsWithMethod<Paths, Method>,
    Init extends MaybeOptionalInit<Paths[Path], Method>,
    T = Paths[Path][Method],
    Options = Init,
    Data = ParseAsResponse<SuccessResponse<ResponseObjectMap<T>, Media>, Options>
  >(
    pathname: Path,
    options: Options
  ): UseApiReturnType<Data> => {
    const [problemDetails, setProblemDetails] = useState<ProblemDetails | undefined>();
    const [loading, setLoading] = useState(false);
    const [success, setSuccess] = useState<boolean | null>(null);
    const [data, setData] = useState<Data | undefined>();
    const fetchDataRef = useRef<((invalidateCache: boolean) => void) | undefined>();
    const optionsHash = JSON.stringify(options);

    const refresh = useCallback(() => {
      if (fetchDataRef.current) fetchDataRef.current(true);
    }, []);

    // biome-ignore lint/correctness/useExhaustiveDependencies: We use the options hash to detect changes
    useEffect(() => {
      let requestPending = false;
      const abortController = new AbortController();

      const fetchData = async (invalidateCache = false) => {
        if (abortController.signal.aborted || requestPending) return;
        requestPending = true;
        setSuccess(null);
        setLoading(true);
        try {
          const data = await apiMethod(pathname, {
            ...options,
            cache: invalidateCache ? "reload" : "default",
            signal: abortController.signal
            // biome-ignore lint/suspicious/noExplicitAny: We don't know the type at this point but expose a type-safe API
          } as any);
          if (!abortController.signal.aborted) {
            setData(data as Data);
            setSuccess(true);
          }
        } catch (error) {
          if (!abortController.signal.aborted) setSuccess(false);
          if (error instanceof ProblemDetailsError) {
            // The api client always throws ProblemDetailsError
            if (!abortController.signal.aborted) setProblemDetails(error.details);
          } else {
            // Unexpected error in the client
            throw error;
          }
        } finally {
          if (!abortController.signal.aborted) {
            requestPending = false;
            setLoading(false);
            fetchDataRef.current = fetchData;
          }
        }
      };

      fetchData();

      return () => {
        fetchDataRef.current = undefined;
        abortController.abort();
      };
    }, [pathname, optionsHash]);

    return {
      loading,
      success,
      error: problemDetails,
      data,
      refresh
    };
  };
}

export type PlatformApiReactHook<
  // biome-ignore lint/complexity/noBannedTypes: This is the exact type used in "openapi-typescript"
  Paths extends Record<string, Record<HttpMethod, {}>>,
  Method extends HttpMethod,
  Media extends MediaType = MediaType
> = ReturnType<typeof createApiReactHook<Paths, Method, Media>>;
