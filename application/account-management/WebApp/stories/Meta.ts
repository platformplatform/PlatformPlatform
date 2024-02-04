export interface Meta<T> {
  component: T;
  parameters?: Record<string, unknown>;
  tags?: string[];
  args?: Record<string, unknown>;
}
