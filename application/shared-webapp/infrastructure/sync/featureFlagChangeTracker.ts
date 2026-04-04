let featureFlagChanged = false;

export function markFeatureFlagChanged(): void {
  featureFlagChanged = true;
}

export function getAndClearFeatureFlagChanged(): boolean {
  const value = featureFlagChanged;
  featureFlagChanged = false;
  return value;
}
