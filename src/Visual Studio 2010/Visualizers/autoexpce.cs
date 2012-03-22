// This file contains DisplayAttribute defines for types from Windows CE assemblies
// If any changes are made, its needs to be recompiled while referencing CE assemblies

#region Using directives

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security;
using WinForms = System.Windows.Forms;
using SQLTypes = System.Data.SqlTypes;

#endregion

// mscorlib
[assembly: DebuggerDisplay(@"\{Name = {Name} FullName = {FullName}}", Target = typeof(Type))]

// System.Drawing
[assembly: DebuggerDisplay(@"\{Name = {Name} Size={Size}}", Target = typeof(Font))]
[assembly: DebuggerDisplay(@"\{Name = {m_strName}}", Target = typeof(FontFamily))]
[assembly: DebuggerDisplay(@"\{Color = {m_color}}", Target = typeof(Pen))]
[assembly: DebuggerDisplay(@"\{X = {m_x} Y = {m_y}}", Target = typeof(Point))]
[assembly: DebuggerDisplay(@"\{X = {m_x} Y = {m_y} Width = {m_cx} Height = {m_cy}}", Target = typeof(Rectangle))]
[assembly: DebuggerDisplay(@"\{X = {m_x} Y = {m_y} Width = {m_cx} Height = {m_cy}}", Target = typeof(RectangleF))]
[assembly: DebuggerDisplay(@"\{Width = {m_cx} Height = {m_cy}}", Target = typeof(Size))]
[assembly: DebuggerDisplay(@"\{Width = {m_cx} Height = {m_cy}}", Target = typeof(SizeF))]
[assembly: DebuggerDisplay(@"\{Color = {m_color}}", Target = typeof(SolidBrush))]



// System.Windows.Forms
[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WinForms::Button))]
[assembly: DebuggerDisplay(@"\{Text = {Text} CheckState = {CheckState}}", Target = typeof(WinForms::CheckBox))]
[assembly: DebuggerDisplay(@"\{Value = {Value} Min = {Minimum} Max = {Maximum}}", Target = typeof(WinForms::HScrollBar))]
[assembly: DebuggerDisplay(@"\{Index = {index}}", Target = typeof(WinForms::ItemChangedEventArgs))]
[assembly: DebuggerDisplay(@"\{Index = {m_iIndex} NewValue = {m_newCheckValue} CurrentValue = {m_CurrentValue}}", Target = typeof(WinForms::ItemCheckEventArgs))]
[assembly: DebuggerDisplay(@"\{KeyData = {m_keyData}}", Target = typeof(WinForms::KeyEventArgs))]
[assembly: DebuggerDisplay(@"\{KeyChar = {m_ch}}", Target = typeof(WinForms::KeyPressEventArgs))]
[assembly: DebuggerDisplay(@"\{SelectedItem = {Text}}", Target = typeof(WinForms::ListBox))]
[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WinForms::ListViewItem))]
[assembly: DebuggerDisplay(@"\{X = {m_x} Y = {m_y} Button = {button}}", Target = typeof(WinForms::MouseEventArgs))]
[assembly: DebuggerDisplay(@"\{Value = {Value} Min = {Minimum} Max = {Maximum}}", Target = typeof(WinForms::NumericUpDown))]
[assembly: DebuggerDisplay(@"\{ClipRectangle = {m_rcClip}}", Target = typeof(WinForms::PaintEventArgs))]
[assembly: DebuggerDisplay(@"\{Value = {Value} Min = {Minimum} Max = {Maximum}}", Target = typeof(WinForms::ProgressBar))]
[assembly: DebuggerDisplay(@"\{Text = {Text} Checked = {Checked}}", Target = typeof(WinForms::RadioButton))]
[assembly: DebuggerDisplay(@"\{Bounds = {Bounds} WorkingArea = {WorkingArea}}", Target = typeof(WinForms::Screen))]
[assembly: DebuggerDisplay(@"\{MinExtra = {MinExtra} MinSize = {MinSize}}", Target = typeof(WinForms::Splitter))]
[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WinForms::TextBox))]
[assembly: DebuggerDisplay(@"\{Interval = {m_msec}}", Target = typeof(WinForms::Timer))]
[assembly: DebuggerDisplay(@"\{{Value} Min = {Minimum} Max = {Maximum}}", Target = typeof(WinForms::TrackBar))]
[assembly: DebuggerDisplay(@"\{Text = {m_strText}}", Target = typeof(WinForms::TreeNode))]
[assembly: DebuggerDisplay(@"\{{Value} Min = {Minimum} Max = {Maximum}}", Target = typeof(WinForms::VScrollBar))]

[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(Exception))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(ApplicationException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(ArgumentException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(ArgumentNullException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(ArgumentOutOfRangeException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(ArithmeticException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(DivideByZeroException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(DllNotFoundException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(ApplicationException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(IndexOutOfRangeException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(InvalidCastException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(MemberAccessException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(MethodAccessException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(NullReferenceException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(StackOverflowException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(SystemException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(TypeLoadException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(FileNotFoundException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(SecurityException))]

// The attributes below can be useful to uncomment if ToString evaluation is disabled. 
//
//[assembly: DebuggerDisplay("{Month}/{Day}/{Year} {Hour}:{Minute}:{Second}", Target = typeof(DateTime))]
//[assembly: DebuggerDisplay(@"\{Value = {Value}}", Target = typeof(WinForms::DateTimePicker))]
//[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WinForms::LinkLabel))]
//[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WinForms::Label))]
//[assembly: DebuggerDisplay(@"\{{SelectionStack = {SelectionStart} SelectionEnd = {SelectionEnd}}", Target = typeof(WinForms::MonthCalendar))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlInt64))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlDateTime))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlInt32))]
//[assembly: DebuggerDisplay("{Value}", Target = typeof(SQLTypes::SqlMoney))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlString))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlSingle))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlInt16))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlByte))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlDouble))]

namespace System.Diagnostics {

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DebuggerDisplayAttribute : Attribute
    {
        private string name;
        private string value;
        private string type;
        private string targetName;
        private Type target;

        public DebuggerDisplayAttribute(string value)
        {
            if( value == null ) {
                this.value = "";
            }
            else {
                this.value = value;
            }
            name = "";
            type = "";
        }   

        public string Value
        {
            get { return this.value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        public Type Target
        {
            set { 
                if( value == null) {
                    throw new ArgumentNullException("value");
                }
                
                targetName = value.AssemblyQualifiedName; 
                target = value;
            }
            get { return target; }
        }

        public string TargetTypeName
        {
            get { return targetName; }
            set { targetName = value; }
    
        }
    }
}
