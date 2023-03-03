using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ThreadsInSemaphoreSimulation.UserControls;


using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

/// <summary>
/// Interaction logic for NumericUpDown.xaml
/// </summary>
public partial class UC_NumericUpDown : UserControl
{
    /// <summary>
    /// Support binding on property IsReadOnly.
    /// </summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(UC_NumericUpDown),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(UC_NumericUpDown.OnIsReadOnlyChanged)));

    /// <summary>
    /// Support binding on property Value.
    /// </summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(decimal), typeof(UC_NumericUpDown),
        new FrameworkPropertyMetadata(0M, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(UC_NumericUpDown.OnValueChanged), new CoerceValueCallback(UC_NumericUpDown.OnCoerceValue)));

    private static readonly DoubleAnimation errorAnimation = new DoubleAnimation(0d, 1d, new Duration(TimeSpan.FromSeconds(0.5d)));

    /// <summary>
    /// Locker object for increment/decrement + repeater property.
    /// </summary>
    private readonly object locker = new object();

    /// <summary>
    /// doCoerce = a value indicating whether to do coercing during callback; doGotFocus = a value indicating whether to do GotFocus.
    /// </summary>
    private bool doCoerce = true, doGotFocus = true;

    /// <summary>
    /// Number of decimal places to show.
    /// </summary>
    private int decimalPlaces;

    /// <summary>
    /// The format used to display the decimal value.
    /// </summary>
    private string decimalFormat = "0";

    /// <summary>
    /// Minimum, Maximum and Increment property values.
    /// </summary>
    private decimal min = 0M, max = 100M, inc = 1M;

    static UC_NumericUpDown()
    {
        UC_NumericUpDown.errorAnimation.AutoReverse = true;
        if (UC_NumericUpDown.errorAnimation.CanFreeze && !UC_NumericUpDown.errorAnimation.IsFrozen) UC_NumericUpDown.errorAnimation.Freeze();
    }

    /// <summary>
    /// Initializes a new instance of the NumericUpDown class.
    /// </summary>
    public UC_NumericUpDown()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Value changed event handler.
    /// </summary>
    public event EventHandler ValueChanged;

    /// <summary>
    /// Gets a value indicating whether the user control is focused.
    /// </summary>
    public bool IsUserControlFocused
    {
        get { return this.textBoxValue.IsFocused; }
    }

    /// <summary>
    /// Gets or sets the value of decimal places to show.
    /// </summary>
    /// <remarks>If the Increment property value has too many decimal places then it will be upscaled to match the new decimal places. Value will be automatically constrained to 0-28.</remarks>
    public int DecimalPlaces
    {
        get
        {
            return this.decimalPlaces;
        }

        set
        {
            this.decimalPlaces = Math.Max(0, Math.Min(28, value));

            StringBuilder format = new StringBuilder("0");

            if (this.decimalPlaces > 0)
            {
                format.Append(".");
                for (int i = 0; i < this.decimalPlaces; i++)
                {
                    format.Append("0");
                }
            }

            this.decimalFormat = format.ToString();

            string incr = this.inc.ToString(CultureInfo.InvariantCulture);
            int decimalIndex = incr.IndexOf(".", StringComparison.Ordinal);

            if (incr.Contains(".") && incr.Substring(decimalIndex + 1).Length > this.decimalPlaces) this.UpscaleIncrement(incr, decimalIndex);

            // double rounding occurs, but that's no big deal. Can be fixed with this.Value = this.Value, but that looks wiered...
            if (this.Value.ToString(CultureInfo.InvariantCulture).Contains(".")) this.Value = Math.Round(this.Value, this.decimalPlaces, MidpointRounding.AwayFromZero);

            this.UpdateTextBox();

            // set min and max with decimal places, they will also do SetTextBoxMaxLength
            this.Minimum = this.min;
            this.Maximum = this.max;
        }
    }

    /// <summary>
    /// Gets or sets the incrementing value.
    /// </summary>
    /// <remarks>If the supplied value has too many decimal places then an upscaled value will be used instead (eg: DecimalPlaces = 2; Supplied value = 0.0005; Upscaled value = 0.01).
    /// Value will be automatically constrained to > 0.</remarks>
    public decimal Increment
    {
        get
        {
            return this.inc;
        }

        set
        {
            if (value <= 0) this.inc = 1M;
            else
            {
                string val = value.ToString(CultureInfo.InvariantCulture);

                int decimalIndex = val.IndexOf(".", StringComparison.Ordinal);

                if (val.Contains(".") && val.Substring(decimalIndex + 1).Length > this.decimalPlaces) this.UpscaleIncrement(val, decimalIndex);
                else this.inc = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the control is readonly
    /// </summary>
    public bool IsReadOnly
    {
        get
        {
            return (bool)this.GetValue(UC_NumericUpDown.IsReadOnlyProperty);
        }

        set
        {
            this.SetCurrentValue(UC_NumericUpDown.IsReadOnlyProperty, value);
        }
    }

    /// <summary>
    /// Gets or sets the minimum selectable value.
    /// </summary>
    /// <remarks>If provided value bigger than Maximum then Maximum is set to Minimum.</remarks>
    public decimal Minimum
    {
        get
        {
            return this.min;
        }

        set
        {
            this.min = Math.Round(value, this.DecimalPlaces, MidpointRounding.AwayFromZero);

            if (this.min > this.max) this.Maximum = this.min; // Maximum does SetTextBoxMaxLength
            else this.SetTextBoxMaxLength();

            if (this.Value < this.min) this.Value = this.min;
        }
    }

    /// <summary>
    /// Gets or sets the maximum selectable value.
    /// </summary>
    /// <remarks>If provided value smaller than Minimum then Maximum is set to Minimum.</remarks>
    public decimal Maximum
    {
        get
        {
            return this.max;
        }

        set
        {
            this.max = Math.Max(this.min, Math.Round(value, this.DecimalPlaces, MidpointRounding.AwayFromZero)); // Math.Max => max not smaller than min

            if (this.Value > this.max) this.Value = this.max;

            this.SetTextBoxMaxLength();
        }
    }

    /// <summary>
    /// Gets or sets the textBoxValue TextAlignment
    /// </summary>
    public TextAlignment TextAlignment
    {
        get
        {
            return this.textBoxValue.TextAlignment;
        }

        set
        {
            this.textBoxValue.TextAlignment = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to show a context menu when right clicking on the textbox.
    /// </summary>
    public bool ShowContextMenu
    {
        get
        {
            return ContextMenuService.GetIsEnabled(this.textBoxValue);
        }

        set
        {
            ContextMenuService.SetIsEnabled(this.textBoxValue, value);
        }
    }

    /// <summary>
    /// Gets or sets the spinner (up/down arrow) width. Should be an uneven number for perfect spacing. (Minimum value = 17d)
    /// </summary>
    

    /// <summary>
    /// Gets or sets the NumericUpDown value.
    /// </summary>
    public decimal Value
    {
        get
        {
            return (decimal)this.GetValue(UC_NumericUpDown.ValueProperty);
        }

        set
        {
            // do a manual coerce here, inorder for source to never get an out-of-sync value (otherwise gets bad value, then coerce callback comes and sets it back).
            this.doCoerce = false;
            this.SetCurrentValue(UC_NumericUpDown.ValueProperty, UC_NumericUpDown.Coerce(this.Minimum, this.Maximum, value, this.DecimalPlaces));
            this.doCoerce = true;
        }
    }

    /// <summary>
    /// Selects the entire text in the internal textbox.
    /// </summary>
    public void SelectTextBoxText()
    {
        // this bit required (unlike TimePicker), because textbox is not read only?
        if (this.IsTabStop == false)
        {
            this.doGotFocus = false;
            this.textBoxValue.Focus();
            this.doGotFocus = true;
        }

        this.textBoxValue.Select(0, this.textBoxValue.Text.Length);
    }

    /// <summary>
    /// On IsReadOnly changed event handler.
    /// </summary>
    /// <param name="property">Dependency object.</param>
    /// <param name="args">Arguments supplied.</param>
    private static void OnIsReadOnlyChanged(DependencyObject property, DependencyPropertyChangedEventArgs args)
    {
        bool value = (bool)args.NewValue;
        UC_NumericUpDown nupd = (UC_NumericUpDown)property;

        nupd.textBoxValue.IsReadOnly = value;
        nupd.repeatButtonUp.IsEnabled = nupd.repeatButtonDown.IsEnabled = !value;
    }

    /// <summary>
    /// On value changed event handler.
    /// </summary>
    /// <param name="property">Dependency object.</param>
    /// <param name="args">Arguments supplied.</param>
    private static void OnValueChanged(DependencyObject property, DependencyPropertyChangedEventArgs args)
    {
        //// in XAML designer, if you remove Value="", then OnValueChanged is called, but no CoerceValue callback, thus there is a tiny mismatch between Min/Max and Value)

        UC_NumericUpDown nupd = (UC_NumericUpDown)property;
        nupd.UpdateTextBox();

        // if someone is listening to the value changed event and value differs then notify them
        if ((decimal)args.OldValue != (decimal)args.NewValue && nupd.ValueChanged != null) nupd.ValueChanged(nupd, new EventArgs());
    }

    /// <summary>
    /// Coerces the given value between Minimum and Maximum.
    /// </summary>
    /// <param name="property">Dependency object.</param>
    /// <param name="value">Value to be coerced.</param>
    /// <returns>The coerced value.</returns>
    private static object OnCoerceValue(DependencyObject property, object value)
    {
        UC_NumericUpDown nupd = (UC_NumericUpDown)property;
        decimal origValue = (decimal)value, newValue = origValue;

        // this is only done on initialization (this.Value is bypassed by .NET FX) and when bindings get changed
        if (nupd.doCoerce)
        {
            newValue = UC_NumericUpDown.Coerce(nupd.Minimum, nupd.Maximum, origValue, nupd.DecimalPlaces);

            // CoerceValue callback is called after source has been updated, inorder to get source back in sync, I must manually update the source.
            // The only way to do this is via reflection. Creating another binding will throw a StackOverflowException, using Dispatcher.BeginInvoke will hang Visual Studio designer...
            if (newValue != origValue)
            {
                try
                {
                    var be = nupd.GetBindingExpression(UC_NumericUpDown.ValueProperty);
                    if (be != null && be.DataItem != null && be.ParentBinding != null && be.ParentBinding.Path != null)
                    {
                        object currObj = be.DataItem;
                        Type currType = currObj.GetType();
                        System.Reflection.PropertyInfo currProperty = null;
                        string[] paths = be.ParentBinding.Path.Path.Split(new string[] { ".", "[", "]" }, StringSplitOptions.RemoveEmptyEntries);

                        for (int i = 0; i < paths.Length; i++)
                        {
                            string currPath = paths[i];

                            if (currType.IsArray)
                            {
                                string[] indices = currPath.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                                long[] longIndices = new long[indices.Length]; // I'm using long incase some indexer is of long type (highly unlikely), long is at least compatible with int

                                for (int j = 0; j < indices.Length; j++)
                                {
                                    longIndices[j] = Convert.ToInt64(indices[j]);
                                }

                                Array arr = (Array)currObj;
                                currObj = arr.GetValue(longIndices);
                                currType = currObj.GetType();

                                if (i == paths.Length - 1) arr.SetValue(Convert.ChangeType(newValue, currType, CultureInfo.InvariantCulture), longIndices);
                            }
                            else
                            {
                                currProperty = currType.GetProperty(currPath);

                                // I can't set currObj until I know that the loop continues, I need it for value setting
                                object newObj = currProperty.GetValue(currObj, null);
                                currType = newObj.GetType();

                                if (i == paths.Length - 1) currProperty.SetValue(currObj, Convert.ChangeType(newValue, currType, CultureInfo.InvariantCulture), null);
                                else currObj = newObj;
                            }
                        }
                    }
                }
                catch (Exception ex) // possible exceptions include XAML errors (wrong Path) and perhaps custom indexers
                {
                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                }
            }
        }

        if (nupd.Value == newValue) nupd.UpdateTextBox(); // if values are same, OnValueChanged will not be called.

        return newValue;
    }

    private static decimal Coerce(decimal min, decimal max, decimal value, int decPlaces)
    {
        return Math.Max(min, Math.Min(max, Math.Round(value, decPlaces, MidpointRounding.AwayFromZero)));
    }

    private void ucNUPD_GotFocus(object sender, RoutedEventArgs e)
    {
        // select only if tabbed into or focus set via code. Also ignore gotfocus from repeater button click.
        if (!this.IsReadOnly && this.doGotFocus && !(this.IsMouseOver && Mouse.LeftButton == MouseButtonState.Pressed)) this.SelectTextBoxText();
    }

    private void repeatButtonUp_Click(object sender, RoutedEventArgs e)
    {
        this.IncrementValue();
    }

    private void repeatButtonDown_Click(object sender, RoutedEventArgs e)
    {
        this.DecrementValue();
    }

    private void repeatButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        this.SelectTextBoxText();
    }

    private void textBoxValue_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        try
        {
            if (e.Text.Length == 1 && (this.textBoxValue.Text.Length < this.textBoxValue.MaxLength || this.textBoxValue.SelectionLength >= 1))
            {
                if ((Char.IsDigit(e.Text, 0) ||
                    (e.Text == NumberFormatInfo.CurrentInfo.NumberDecimalSeparator && this.decimalPlaces > 0 &&
                    !this.textBoxValue.Text.Contains(e.Text) && this.textBoxValue.SelectionStart >= 1) ||
                    (e.Text == NumberFormatInfo.CurrentInfo.NegativeSign && !this.textBoxValue.Text.Contains(e.Text) && this.textBoxValue.SelectionStart == 0)) == false)
                {
                    e.Handled = true; // do not accept input if it has nothing to do with decimals
                }
            }
            else e.Handled = true;
        }
        catch (FormatException)
        {
            e.Handled = true;
        }
        catch (OverflowException)
        {
            e.Handled = true;
        }
    }

    private void textBoxValue_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        try
        {
            string input = (string)e.DataObject.GetData(typeof(string));
            string currentText = (this.textBoxValue.SelectionLength > 0 ?
                this.textBoxValue.Text.Remove(this.textBoxValue.SelectionStart, this.textBoxValue.SelectionLength) : this.textBoxValue.Text);

            decimal d = Convert.ToDecimal(currentText.Insert(this.textBoxValue.SelectionStart, input), CultureInfo.CurrentCulture);
            this.Value = d;
        }
        catch (FormatException)
        {
            this.borderError.BeginAnimation(TextBox.OpacityProperty, UC_NumericUpDown.errorAnimation, HandoffBehavior.SnapshotAndReplace);
        }
        catch (OverflowException)
        {
            this.borderError.BeginAnimation(TextBox.OpacityProperty, UC_NumericUpDown.errorAnimation, HandoffBehavior.SnapshotAndReplace);
        }
        finally
        {
            e.CancelCommand();
        }
    }

    private void textBoxValue_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up && !this.IsReadOnly) this.IncrementValue();
        else if (e.Key == Key.Down && !this.IsReadOnly) this.DecrementValue();
        else if (e.Key == Key.Enter) this.UpdateValue();
        else if (e.Key == Key.Space) e.Handled = true;
    }

    private void textBoxValue_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Up || e.Key == Key.Down) this.SelectTextBoxText();
    }

    private void textBoxValue_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        this.textBoxValue.SelectAll();
    }

    private void textBoxValue_LostFocus(object sender, RoutedEventArgs e)
    {
        this.UpdateValue();
    }

    private void IncrementValue()
    {
        lock (this.locker)
        {
            this.Value = this.Value + this.Increment;
        }
    }

    private void DecrementValue()
    {
        lock (this.locker)
        {
            this.Value = this.Value - this.Increment;
        }
    }

    /// <summary>
    /// Updates the value after text has been typed.
    /// </summary>
    private void UpdateValue()
    {
        try
        {
            string currentText = this.textBoxValue.Text;

            if (this.decimalPlaces > 0 && !currentText.Contains(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator))
            {
                StringBuilder sb = new StringBuilder(currentText);
                sb.Append(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator);

                for (int i = 0; i < this.decimalPlaces; i++)
                {
                    sb.Append("0");
                }

                currentText = sb.ToString();
            }

            decimal d = Convert.ToDecimal(currentText, CultureInfo.CurrentCulture);

            if (this.Value == d) this.UpdateTextBox(); // reset text to a correct value (it might be incorrect)
            else this.Value = d;
        }
        catch (FormatException)
        {
            this.borderError.BeginAnimation(TextBox.OpacityProperty, UC_NumericUpDown.errorAnimation, HandoffBehavior.SnapshotAndReplace);
            this.UpdateTextBox(); // wrong chars, so set it back to normal
        }
        catch (OverflowException)
        {
            this.borderError.BeginAnimation(TextBox.OpacityProperty, UC_NumericUpDown.errorAnimation, HandoffBehavior.SnapshotAndReplace);

            try
            {
                string firstChar = this.textBoxValue.Text.Substring(0, 1);

                if (firstChar == NumberFormatInfo.CurrentInfo.NegativeSign) this.Value = this.min;
                else this.Value = this.max;
            }
            catch (Exception)
            {
                this.UpdateTextBox(); // if in some mystical case text is empty
            }
        }
    }

    /// <summary>
    /// Updates the text box.
    /// </summary>
    private void UpdateTextBox()
    {
        this.textBoxValue.Text = this.Value.ToString(this.decimalFormat, CultureInfo.CurrentCulture);

        // ignore selection start during repeat button pressed
        if (!(this.IsMouseOver && Mouse.LeftButton == MouseButtonState.Pressed)) this.textBoxValue.SelectionStart = this.textBoxValue.Text.Length;
    }

    private void UpscaleIncrement(string val, int decimalIndex)
    {
        string newVal = val.Substring(0, decimalIndex + 1 + this.decimalPlaces);

        StringBuilder sb = new StringBuilder("0.");

        for (int i = 0; i < this.decimalPlaces - 1; i++)
        {
            sb.Append("0");
        }

        sb.Append("1");

        // If DecimalPlaces = 2 and value = 28.005 then increment will be 28.00, if value = 0.0005 then increment wil be 0.01
        this.inc = Math.Max(Convert.ToDecimal(newVal, CultureInfo.InvariantCulture), Convert.ToDecimal(sb.ToString(), CultureInfo.InvariantCulture));
    }

    private void SetTextBoxMaxLength()
    {
        this.textBoxValue.MaxLength = Math.Max(this.max.ToString(this.decimalFormat, CultureInfo.CurrentCulture).Length, this.min.ToString(this.decimalFormat, CultureInfo.CurrentCulture).Length);
    }
}