import { Prop, PropList, PropNote } from "./PropTooltip";

export const tooltips = {
  textField: (
    <PropList title="TextField" description="Single-line text input">
      <Prop name="placeholder">Ghost text when empty</Prop>
      <Prop name="startIcon">Icon before the input</Prop>
      <Prop name="required">Marks field as required</Prop>
      <Prop name="disabled">Greyed out, not interactive</Prop>
      <Prop name="readOnly">Visible but not editable</Prop>
    </PropList>
  ),
  textArea: (
    <PropList title="TextAreaField" description="Multi-line text input">
      <Prop name="placeholder">Ghost text when empty</Prop>
      <Prop name="lines">Fixed height in lines (disables auto-grow)</Prop>
      <Prop name="resizable">Allow user to resize (default true)</Prop>
      <PropNote>Auto-grows with content when no fixed line count is set.</PropNote>
    </PropList>
  ),
  textAreaFixed: (
    <PropList title="TextAreaField" description="Fixed-height, non-resizable">
      <Prop name="lines" value="2">
        Exactly 2 lines tall
      </Prop>
      <Prop name="resizable" value="false">
        User cannot resize
      </Prop>
      <PropNote>Good for addresses and other short multi-line fields.</PropNote>
    </PropList>
  ),
  numberInteger: (
    <PropList title="NumberField" description="Numeric input with stepper buttons">
      <Prop name="step" value="1">
        Increment per click or key press
      </Prop>
      <Prop name="minValue" value="0">
        Minimum allowed value
      </Prop>
      <Prop name="maxValue" value="100">
        Maximum allowed value
      </Prop>
      <Prop name="allowEmpty">Permits empty field (null value)</Prop>
      <PropNote>Arrow keys and long-press on buttons accelerate over time.</PropNote>
    </PropList>
  ),
  numberDecimal: (
    <PropList title="NumberField" description="Decimal variant with locale formatting">
      <Prop name="step" value="0.1">
        Increment by 0.1
      </Prop>
      <Prop name="decimalPlaces" value="2">
        Always display 2 decimal places
      </Prop>
      <Prop name="minValue" value="0">
        Minimum allowed value
      </Prop>
      <Prop name="maxValue" value="999.99">
        Maximum allowed value
      </Prop>
      <PropNote>Decimal separator adapts to locale (e.g. comma for Danish).</PropNote>
    </PropList>
  ),
  select: (
    <PropList title="SelectField" description="Dropdown for selecting one value">
      <Prop name="items">Options with value, label, and optional icon</Prop>
      <Prop name="value">Currently selected value</Prop>
      <Prop name="onValueChange">Callback when selection changes</Prop>
      <PropNote>Supports icons in both the dropdown items and the collapsed trigger.</PropNote>
    </PropList>
  ),
  multiSelect: (
    <PropList title="MultiSelect" description="Select multiple values from a list">
      <Prop name="items">Items with id, label, and optional icon</Prop>
      <Prop name="value">Array of selected item ids</Prop>
      <Prop name="onChange">Callback with updated id array</Prop>
      <PropNote>Arrow keys navigate options. Tab closes the dropdown.</PropNote>
    </PropList>
  ),
  combobox: (
    <PropList title="ComboboxField" description="Searchable dropdown with type-to-filter">
      <Prop name="items">Filterable options with id, label, icon</Prop>
      <Prop name="emptyMessage">Text shown when filter has no matches</Prop>
      <PropNote>Only allows selecting existing values. Auto-selects on exact text match.</PropNote>
    </PropList>
  ),
  comboboxFreeText: (
    <PropList title="ComboboxField" description="Free text with suggestions">
      <Prop name="allowCustomValue">Accepts values not in the items list</Prop>
      <PropNote>Works like a text input with autocomplete suggestions. Typed value is kept on blur.</PropNote>
    </PropList>
  ),
  comboboxCreatable: (
    <PropList title="ComboboxField" description="Creatable with explicit add option">
      <Prop name="allowCreate">Shows a Create option for new values</Prop>
      <Prop name="onCreateItem">Callback when a new item is created</Prop>
      <PropNote>Use when new options should be explicitly added to the list.</PropNote>
    </PropList>
  ),
  dateField: (
    <PropList title="DateField" description="Native browser date input">
      <Prop name="min / max">Restrict selectable date range</Prop>
      <PropNote>Uses the browser's built-in date picker UI. Format follows browser locale.</PropNote>
    </PropList>
  ),
  datePicker: (
    <PropList title="DatePicker" description="Custom calendar popup for selecting a date">
      <Prop name="value / onChange">Controlled date as YYYY-MM-DD string</Prop>
      <Prop name="min / max">Restrict selectable range</Prop>
      <Prop name="showDropdowns">Month/year dropdown navigation</Prop>
      <PropNote>Shows full date format. Clearable with X button.</PropNote>
    </PropList>
  ),
  workdayPicker: (
    <PropList title="DatePicker (constrained)" description="Bounded range, per-date predicate, relative display">
      <Prop name="min / max">Selectable from today − 7 days through today + 30 days</Prop>
      <Prop name="disabledDate">Predicate — works as a deny-list (e.g. weekends) or an allow-list (just invert)</Prop>
      <Prop name='displayFormat="relative"'>Renders as Today / Yesterday / Tomorrow / In N days</Prop>
    </PropList>
  ),
  dateInput: (
    <PropList title="DateInput" description="Editable date field without the calendar popover">
      <Prop name="value / onChange">Controlled date as YYYY-MM-DD string</Prop>
      <Prop name="displayFormat">Same options as DatePicker (input, short, long, relative, custom)</Prop>
      <PropNote>Same masking and validation as DatePicker. Use when keyboard entry is the only path.</PropNote>
    </PropList>
  ),
  dateRange: (
    <PropList title="DateRangePicker" description="Type endpoints or click two days; clearable">
      <Prop name="value / onChange">DateRangeValue or null</Prop>
      <Prop name="displayFormat">Same options as DatePicker (input, short, long, relative, custom)</Prop>
    </PropList>
  ),
  timeField: (
    <PropList title="TimeField" description="Native browser time input">
      <Prop name="value / onChange">Time as HH:MM string</Prop>
      <Prop name="trailingContent">Icon or content after the input</Prop>
    </PropList>
  ),
  timeZonePicker: (
    <PropList title="TimeZonePicker" description="Searchable IANA time zone selector grouped by continent">
      <Prop name="value / onValueChange">IANA time zone identifier</Prop>
    </PropList>
  ),
  inputOtp: (
    <PropList title="InputOtp" description="Fixed-length one-time-code input">
      <Prop name="maxLength">Number of slots (typically 4 or 6)</Prop>
      <Prop name="value / onChange">Controlled code string</Prop>
      <Prop name="pattern">Regex restricting accepted characters</Prop>
      <PropNote>Accepts paste. Slots auto-advance as digits are entered.</PropNote>
    </PropList>
  ),
  selectWithGroups: (
    <PropList title="SelectField with groups" description="Select with grouped options">
      <Prop name="SelectGroup">Wraps a group of items</Prop>
      <Prop name="SelectLabel">Sticky group label inside the dropdown</Prop>
      <PropNote>Use sticky labels to keep the group visible while scrolling long lists.</PropNote>
    </PropList>
  ),
  switchField: (
    <PropList title="SwitchField" description="Toggle for on/off settings; readOnly disables changes">
      <Prop name="checked / onCheckedChange">Controlled toggle state</Prop>
    </PropList>
  ),
  checkboxField: (
    <PropList title="CheckboxField" description="Tick box for boolean choices; readOnly disables changes">
      <Prop name="checked / onCheckedChange">Controlled check state</Prop>
    </PropList>
  ),
  checkboxIndeterminate: (
    <PropList title="CheckboxField (indeterminate)" description="Minus glyph for mixed state, set programmatically">
      <Prop name="indeterminate">e.g. Select-all with partial selection or bulk-edit with mixed values</Prop>
    </PropList>
  ),
  radioGroup: (
    <PropList title="RadioGroupField" description="Choose one from 2-5 options; use Select for more">
      <Prop name="defaultValue">Initially selected option</Prop>
      <Prop name="readOnly">Visible but not changeable</Prop>
    </PropList>
  ),
  toggleGroup: (
    <PropList title="ToggleGroup" description="Segmented button group for view modes or small exclusive sets">
      <Prop name="variant">default or outline</Prop>
      <Prop name="value">Controlled pressed state as array of values</Prop>
    </PropList>
  ),
  slider: (
    <PropList title="Slider with steps" description="Numeric input that snaps to step increments">
      <Prop name="step">Increment between values, e.g. step=20 gives 5 stops</Prop>
      <Prop name="defaultValue">Array — single [40] or range [20, 80]</Prop>
      <Prop name="min / max">Bounds (default 0 and 100)</Prop>
    </PropList>
  ),
  sliderRange: (
    <PropList title="Slider (range)" description="Two thumbs select an interval">
      <Prop name="defaultValue">e.g. [20, 80]</Prop>
    </PropList>
  )
};
