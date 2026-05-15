// Stable consumer entry point for the generated feature flag labels. The implementation lives in
// `labels.generated.ts`, regenerated on every backend build from FeatureFlags.cs (see the
// GenerateFeatureFlagsManifest target in application/account/Api/Account.Api.csproj). This wrapper
// exists so consumer imports (`@repo/ui/featureFlags/labels`) don't leak the codegen mechanism.
export {
  getFeatureFlagDescription,
  getFeatureFlagLabel,
  getFeatureFlagName,
  getFeatureFlagSourceLabel
} from "./labels.generated";
