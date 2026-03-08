import { t } from "@lingui/core/macro";
import { authSyncService, type UserLoggedInMessage } from "@repo/infrastructure/auth/AuthSyncService";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { useCallback } from "react";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

import { clearLoginState, setLastSubmittedCode } from "./loginState";

function getPreferredTenantId() {
  try {
    const stored = localStorage.getItem("preferred-tenant");
    return stored || null;
  } catch {
    return null;
  }
}

interface UseLoginVerificationOptions {
  emailLoginId: string;
  email: string;
  returnPath: string | undefined;
  onResendSuccess: (validForSeconds: number) => void;
}

export function useLoginVerification({
  emailLoginId,
  email,
  returnPath,
  onResendSuccess
}: UseLoginVerificationOptions) {
  const completeLoginMutation = api.useMutation("post", "/api/account/authentication/email/login/{id}/complete", {
    onSuccess: () => {
      const message: Omit<UserLoggedInMessage, "timestamp"> = {
        type: "USER_LOGGED_IN",
        userId: "",
        tenantId: getPreferredTenantId() || "",
        email: email || ""
      };
      authSyncService.broadcast(message);

      clearLoginState();
      window.location.href = returnPath || loggedInPath;
    }
  });

  const resendLoginCodeMutation = api.useMutation("post", "/api/account/authentication/email/login/{id}/resend-code", {
    onSuccess: (data) => {
      if (data) {
        onResendSuccess(data.validForSeconds);
        toast.success(t`Verification code sent`, {
          description: t`A new verification code has been sent to your email.`
        });
      }
    }
  });

  const submitVerification = useCallback(
    (code: string) => {
      if (!emailLoginId) {
        return;
      }
      setLastSubmittedCode(code);
      completeLoginMutation.mutate({
        params: {
          path: { id: emailLoginId }
        },
        body: {
          oneTimePassword: code,
          preferredTenantId: getPreferredTenantId() || null
        }
      });
    },
    [completeLoginMutation, emailLoginId]
  );

  return { completeLoginMutation, resendLoginCodeMutation, submitVerification };
}
