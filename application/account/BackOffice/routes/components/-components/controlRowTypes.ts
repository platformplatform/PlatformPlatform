export type ControlRowProps = {
  suffix: string;
  label?: boolean;
  tooltip?: boolean;
  disabled?: boolean;
  readOnly?: boolean;
  error?: boolean;
  showIcon?: boolean;
  values?: boolean;
  placeholders?: boolean;
};

export type ControlRowDerivedProps = ControlRowProps & {
  hasValues: boolean;
  errorMessage: string | undefined;
};
