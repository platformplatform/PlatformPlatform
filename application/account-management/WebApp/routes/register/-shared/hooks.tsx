import { accountManagementApi } from "@/shared/lib/api/client";
import { useEffect, useState } from "react";

export function useIsSubdomainFree(subdomain: string) {
  const [isSubdomainFree, setIsSubdomainFree] = useState<boolean | null>(null);

  useEffect(() => {
    if (subdomain.length < 3) {
      setIsSubdomainFree(null);
      return;
    }

    const abortController = new AbortController();

    const checkSubdomain = async () => {
      const { response, data, error } = await accountManagementApi.GET(
        "/api/account-management/account-registrations/is-subdomain-free",
        {
          params: { query: { Subdomain: subdomain } },
          signal: abortController.signal
        }
      );

      if (!response.ok || error) {
        if (!abortController.signal.aborted) {
          setIsSubdomainFree(null);
        }
        console.error(error ?? response.statusText);
        return;
      }

      if (!abortController.signal.aborted) {
        setIsSubdomainFree(data === true);
      }
      console.log(data);
    };

    const timeout = setTimeout(checkSubdomain, 500);

    return () => {
      abortController.abort();
      clearTimeout(timeout);
    };
  }, [subdomain]);

  return isSubdomainFree;
}
