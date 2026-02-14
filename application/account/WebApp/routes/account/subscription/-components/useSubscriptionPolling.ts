import { t } from "@lingui/core/macro";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import { api, type Schemas } from "@/shared/lib/api/client";

const WebhookPollIntervalMs = 1000;
const WebhookTimeoutMs = 15_000;

type SubscriptionData = Schemas["SubscriptionResponse"];

export function useSubscriptionPolling() {
  const queryClient = useQueryClient();
  const [isPolling, setIsPolling] = useState(false);
  const checkFnRef = useRef<((subscription: SubscriptionData) => boolean) | null>(null);
  const successMessageRef = useRef<string>("");
  const onCompleteRef = useRef<(() => void) | null>(null);

  const { data: subscription } = api.useQuery(
    "get",
    "/api/account/subscriptions/current",
    {},
    { refetchInterval: isPolling ? WebhookPollIntervalMs : false }
  );

  function startPolling(options: {
    check: (subscription: SubscriptionData) => boolean;
    successMessage: string;
    onComplete?: () => void;
  }) {
    checkFnRef.current = options.check;
    successMessageRef.current = options.successMessage;
    onCompleteRef.current = options.onComplete ?? null;
    setIsPolling(true);
    queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
  }

  useEffect(() => {
    if (!isPolling || !subscription) {
      return;
    }
    if (checkFnRef.current?.(subscription)) {
      setIsPolling(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(successMessageRef.current);
      onCompleteRef.current?.();
    }
  }, [isPolling, subscription, queryClient]);

  useEffect(() => {
    if (!isPolling) {
      return;
    }
    const timeout = setTimeout(() => {
      setIsPolling(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.info(t`Your changes are being processed. Please refresh if you don't see the update.`);
      onCompleteRef.current?.();
    }, WebhookTimeoutMs);
    return () => clearTimeout(timeout);
  }, [isPolling, queryClient]);

  return { isPolling, startPolling, subscription };
}
