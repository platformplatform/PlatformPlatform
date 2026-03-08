import { t } from "@lingui/core/macro";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { preferredLocaleKey } from "@repo/infrastructure/translations/constants";
import { useCallback } from "react";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

import { clearSignupState, setLastSubmittedCode } from "./signupState";

interface UseSignupVerificationOptions {
  emailLoginId: string;
  onResendSuccess: (validForSeconds: number) => void;
}

export function useSignupVerification({ emailLoginId, onResendSuccess }: UseSignupVerificationOptions) {
  const completeSignupMutation = api.useMutation("post", "/api/account/authentication/email/signup/{id}/complete", {
    onSuccess: () => {
      clearSignupState();
      window.location.href = loggedInPath;
    }
  });

  const resendSignupCodeMutation = api.useMutation(
    "post",
    "/api/account/authentication/email/signup/{id}/resend-code",
    {
      onSuccess: (data) => {
        if (data) {
          onResendSuccess(data.validForSeconds);
          toast.success(t`Verification code sent`, {
            description: t`A new verification code has been sent to your email.`
          });
        }
      }
    }
  );

  const submitVerification = useCallback(
    (code: string) => {
      setLastSubmittedCode(code);
      completeSignupMutation.mutate({
        params: {
          path: { id: emailLoginId }
        },
        body: {
          oneTimePassword: code,
          preferredLocale: localStorage.getItem(preferredLocaleKey) ?? ""
        }
      });
    },
    [completeSignupMutation, emailLoginId]
  );

  return { completeSignupMutation, resendSignupCodeMutation, submitVerification };
}
