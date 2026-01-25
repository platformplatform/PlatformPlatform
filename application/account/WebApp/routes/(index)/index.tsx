import { Trans } from "@lingui/react/macro";
import { loginPath, signUpPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";
import { Link } from "@repo/ui/components/Link";
import { createFileRoute } from "@tanstack/react-router";
import { PublicFooter } from "@/shared/components/PublicFooter";
import { PublicNavigation } from "@/shared/components/PublicNavigation";

export const Route = createFileRoute("/(index)/")({
  beforeLoad: () => ({ disableAuthSync: true }),
  component: function LandingPage() {
    const isAuthenticated = useIsAuthenticated();

    return (
      <main className="flex min-h-screen w-full flex-col">
        {/* Hero Section */}
        <div className="flex flex-1 flex-col items-center bg-background">
          <PublicNavigation />

          {/* Hero Content */}
          <div className="flex flex-1 flex-col items-center justify-center gap-8 px-6 py-20 text-center">
            <div className="flex max-w-5xl flex-col gap-8">
              {/* Title */}
              <div className="flex flex-col gap-4">
                <h1 className="marketing">
                  <Trans>Welcome to PlatformPlatform</Trans>
                </h1>
                <p className="text-muted-foreground text-xl md:text-2xl">
                  {window.location.hostname === "localhost" ? (
                    <Trans>You successfully installed PlatformPlatform! 🎉</Trans>
                  ) : (
                    <Trans>You successfully deployed PlatformPlatform! 🎉</Trans>
                  )}
                </p>
                <p className="text-base text-muted-foreground md:text-lg">
                  <Trans>Replace this sample page with your own product information and branding.</Trans>
                </p>
              </div>

              {/* CTAs */}
              <div className="flex justify-center gap-4">
                {isAuthenticated ? (
                  <Link href="/account" variant="button-primary" underline={false} className="h-12 rounded-lg px-8">
                    <Trans>Go to app</Trans>
                  </Link>
                ) : (
                  <>
                    <Link href={signUpPath} variant="button-primary" underline={false} className="h-12 rounded-lg px-8">
                      <Trans>Get started</Trans>
                    </Link>
                    <Link
                      href={loginPath}
                      variant="button-secondary"
                      underline={false}
                      className="h-12 rounded-lg px-8"
                    >
                      <Trans>Log in</Trans>
                    </Link>
                  </>
                )}
              </div>
            </div>
          </div>
        </div>

        <PublicFooter />
      </main>
    );
  }
});
