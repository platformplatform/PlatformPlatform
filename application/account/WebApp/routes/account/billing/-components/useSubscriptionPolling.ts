import { t } from "@lingui/core/macro";
import { useSubscription } from "@repo/infrastructure/sync/hooks";
import { useEffect, useRef, useState } from "react";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

const WebhookPollIntervalMs = 1000;
const WebhookTimeoutMs = 15_000;

type SubscriptionData = NonNullable<ReturnType<typeof useSubscription>["data"]>;

export function useSubscriptionPolling() {
  const [isPolling, setIsPolling] = useState(false);
  const checkFnRef = useRef<((subscription: SubscriptionData) => boolean) | null>(null);
  const successMessageRef = useRef<string>("");
  const onCompleteRef = useRef<(() => void) | null>(null);
  const conditionMetRef = useRef(false);

  const { tenantId } = import.meta.user_info_env;
  const { data: subscription } = useSubscription(tenantId ?? "");

  const processPendingEventsMutation = api.useMutation("post", "/api/account/subscriptions/process-pending-events");
  const { mutate: processPendingEvents } = processPendingEventsMutation;

  useEffect(() => {
    if (!isPolling) {
      return;
    }
    const interval = setInterval(() => {
      processPendingEvents({});
    }, WebhookPollIntervalMs);
    processPendingEvents({});
    return () => clearInterval(interval);
  }, [isPolling, processPendingEvents]);

  function startPolling(options: {
    check: (subscription: SubscriptionData) => boolean;
    successMessage: string;
    onComplete?: () => void;
  }) {
    checkFnRef.current = options.check;
    successMessageRef.current = options.successMessage;
    onCompleteRef.current = options.onComplete ?? null;
    conditionMetRef.current = false;
    setIsPolling(true);
  }

  useEffect(() => {
    if (!isPolling || !subscription) {
      return;
    }
    if (checkFnRef.current?.(subscription)) {
      conditionMetRef.current = true;
      setIsPolling(false);
      toast.success(successMessageRef.current);
      onCompleteRef.current?.();
    }
  }, [isPolling, subscription]);

  useEffect(() => {
    if (!isPolling) {
      return;
    }
    const timeout = setTimeout(() => {
      if (!conditionMetRef.current) {
        setIsPolling(false);
        toast.warning(t`Your changes may take a moment to appear.`);
        onCompleteRef.current?.();
      }
    }, WebhookTimeoutMs);
    return () => clearTimeout(timeout);
  }, [isPolling]);

  return { isPolling, isLoading: false, startPolling, subscription };
}
