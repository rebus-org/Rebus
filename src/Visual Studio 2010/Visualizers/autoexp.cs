#region Using directives

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security;
using WebControls = System.Web.UI.WebControls;
using HTMLControls = System.Web.UI.HtmlControls;
using WinForms = System.Windows.Forms;
using SQLTypes = System.Data.SqlTypes;

#endregion

// mscorlib
[assembly: DebuggerDisplay(@"\{Name = {Name} FullName = {FullName}}", Target = typeof(Type))]

// System.Drawing
[assembly: DebuggerDisplay(@"\{Name = {fontFamily.Name} Size={fontSize}}", Target = typeof(Font))]
[assembly: DebuggerDisplay(@"\{Name = {name}}", Target = typeof(FontFamily))]
[assembly: DebuggerDisplay(@"\{Color = {color}}", Target = typeof(Pen))]
[assembly: DebuggerDisplay(@"\{X = {x} Y = {y}}", Target = typeof(Point))]
[assembly: DebuggerDisplay(@"\{X = {x} Y = {y}}", Target = typeof(PointF))]
[assembly: DebuggerDisplay(@"\{X = {x} Y = {y} Width = {width} Height = {height}}", Target = typeof(Rectangle))]
[assembly: DebuggerDisplay(@"\{X = {x} Y = {y} Width = {width} Height = {height}}", Target = typeof(RectangleF))]
[assembly: DebuggerDisplay(@"\{Width = {width} Height = {height}}", Target = typeof(Size))]
[assembly: DebuggerDisplay(@"\{Width = {width} Height = {height}}", Target = typeof(SizeF))]
[assembly: DebuggerDisplay(@"\{Color = {color}}", Target = typeof(SolidBrush))]

// System.Web.UI.WebControls
[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WebControls::Button))]
[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WebControls::Label))]
[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WebControls::HyperLink))]
[assembly: DebuggerDisplay(@"\{Text = {Text} Checked = {Checked}}", Target = typeof(WebControls::CheckBox))]
[assembly: DebuggerDisplay(@"\{Text = {Text} Checked = {Checked}}", Target = typeof(WebControls::RadioButton))]
[assembly: DebuggerDisplay(@"\{SelectedDate = {SelectedData}}", Target = typeof(WebControls::Calendar))]
[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WebControls::LinkButton))]

// System.Web.UI.HtmlControls
[assembly: DebuggerDisplay(@"\{Value = {Value}}", Target = typeof(HTMLControls::HtmlInputButton))]
[assembly: DebuggerDisplay(@"\{InnerText = {InnerText}}", Target = typeof(HTMLControls::HtmlGenericControl))]
[assembly: DebuggerDisplay(@"\{Value = {Value}}", Target = typeof(HTMLControls::HtmlTextArea))]
[assembly: DebuggerDisplay(@"\{Value = {Value}}", Target = typeof(HTMLControls::HtmlInputText))]
[assembly: DebuggerDisplay(@"\{Value = {Value} Checked = {Checked}}", Target = typeof(HTMLControls::HtmlInputCheckBox))]
[assembly: DebuggerDisplay(@"\{Value = {Value} Checked = {Checked}}", Target = typeof(HTMLControls::HtmlInputRadioButton))]

// System.Windows.Forms
[assembly: DebuggerDisplay(@"\{ExecutablePath = {executablePath}}", Target = typeof(WinForms::Application))]
[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WinForms::Button))]
[assembly: DebuggerDisplay(@"\{Text = {Text} CheckState = {checkState}}", Target = typeof(WinForms::CheckBox))]
[assembly: DebuggerDisplay(@"\{SelectedItem = {Text}}", Target = typeof(WinForms::CheckedListBox))]
[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WinForms::DataGrid))]
[assembly: DebuggerDisplay(@"\{Type = {type} Column = {col} Row = {row}}", Target = typeof(WinForms::DataGrid.HitTestInfo))]
[assembly: DebuggerDisplay(@"\{HeaderText = {headerName}}", Target = typeof(WinForms::DataGridColumnStyle))]
[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WinForms::DataGridTextBox))]
[assembly: DebuggerDisplay(@"\{HeaderText = {headerName}}", Target = typeof(WinForms::DataGridTextBoxColumn))]
[assembly: DebuggerDisplay(@"\{Font = {font} Color = {color}}", Target = typeof(WinForms::FontDialog))]
[assembly: DebuggerDisplay(@"\{Value = {value} Min = {minimum} Max = {maximum}}", Target = typeof(WinForms::HScrollBar))]
[assembly: DebuggerDisplay(@"\{InvalidRect = {invalidRect}}", Target = typeof(WinForms::InvalidateEventArgs))]
[assembly: DebuggerDisplay(@"\{Index = {index}}", Target = typeof(WinForms::ItemChangedEventArgs))]
[assembly: DebuggerDisplay(@"\{Index = {index} NewValue = {newValue} CurrentValue = {currentValue}}", Target = typeof(WinForms::ItemCheckEventArgs))]
[assembly: DebuggerDisplay(@"\{KeyData = {keyData}}", Target = typeof(WinForms::KeyEventArgs))]
[assembly: DebuggerDisplay(@"\{KeyChar = {keyChar}}", Target = typeof(WinForms::KeyPressEventArgs))]
[assembly: DebuggerDisplay(@"\{LinkText = {linkText}}", Target = typeof(WinForms::LinkClickedEventArgs))]
[assembly: DebuggerDisplay(@"\{SelectedItem = {Text}}", Target = typeof(WinForms::ListBox))]
[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target= typeof(WinForms::ListViewItem))]
[assembly: DebuggerDisplay(@"\{X = {x} Y = {y} Button = {button}}", Target = typeof(WinForms::MouseEventArgs))]
[assembly: DebuggerDisplay(@"\{Value = {currentValue} Min = {minimum} Max = {maximum}}", Target = typeof(WinForms::NumericUpDown))]
[assembly: DebuggerDisplay(@"\{ClipRectangle = {clipRect}}", Target = typeof(WinForms::PaintEventArgs))]
[assembly: DebuggerDisplay(@"\{Value = {value} Min = {minimum} Max = {maximum}}", Target = typeof(WinForms::ProgressBar))]
[assembly: DebuggerDisplay(@"\{Text = {Text} Checked = {isChecked}}", Target = typeof(WinForms::RadioButton))]
[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WinForms::RichTextBox))]
[assembly: DebuggerDisplay(@"\{Bounds = {bounds} WorkingArea = {workingArea} Primary = {primary} DeviceName = {deviceName}}", Target = typeof(WinForms::Screen))]
[assembly: DebuggerDisplay(@"\{Start = {start} End = {end}}", Target = typeof(WinForms::SelectionRange))]
[assembly: DebuggerDisplay(@"\{SplitPosition = {splitSize} MinExtra = {minExtra} MinSize = {minSize}}", Target = typeof(WinForms::Splitter))]
[assembly: DebuggerDisplay(@"\{SplitX = {splitX} SplitY = {splitY}}", Target = typeof(WinForms::SplitterEventArgs))]
[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WinForms::TextBox))]
[assembly: DebuggerDisplay(@"\{Interval = {interval}}", Target = typeof(WinForms::Timer))]
[assembly: DebuggerDisplay(@"\{Value = {Value} Min = {minimum} Max = {maximum}}", Target = typeof(WinForms::TrackBar))]
[assembly: DebuggerDisplay(@"\{Text = {text}}", Target = typeof(WinForms::TreeNode))]
[assembly: DebuggerDisplay(@"\{Value = {value} Min = {minimum} Max = {maximum}}", Target = typeof(WinForms::VScrollBar))]

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
[assembly: DebuggerDisplay(@"\{{Message}:{TypeName}}", Target = typeof(TypeLoadException))]
[assembly: DebuggerDisplay(@"\{{Message}:{FileName}}", Target = typeof(FileLoadException))]
[assembly: DebuggerDisplay(@"\{{Message}:{FileName}}", Target = typeof(FileNotFoundException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(ReflectionTypeLoadException))]
[assembly: DebuggerDisplay(@"\{{Message}}", Target = typeof(SecurityException))]
[assembly: DebuggerDisplay(@"\{Method = {Method}}", Target = typeof(System.Delegate))]

// The attributes below can be useful to uncomment if ToString evaluation is disabled. 
//
//[assembly: DebuggerDisplay(@"\{RGB = {value}}", Target = typeof(Color))]
//[assembly: DebuggerDisplay("{Month}/{Day}/{Year} {Hour}:{Minute}:{Second}", Target = typeof(DateTime))]
//[assembly: DebuggerDisplay(@"\{Value = {value}}", Target = typeof(WinForms::DateTimePicker))]
//[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WinForms::LinkLabel))]
//[assembly: DebuggerDisplay(@"\{Text = {Text}}", Target = typeof(WinForms::Label))]
//[assembly: DebuggerDisplay(@"\{SelectionStart = {selectionStart} SelectionEnd = {selectionEnd}}", Target = typeof(WinForms::MonthCalendar))]
//[assembly: DebuggerDisplay(@"\{InitialDelay = {InitialDelay} ShowAlways = {ShowAlways}}", Target = typeof(WinForms::ToolTip))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlInt64))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlDateTime))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlInt32))]
//[assembly: DebuggerDisplay("{Value}", Target = typeof(SQLTypes::SqlMoney))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlString))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlSingle))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlInt16))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlByte))]
//[assembly: DebuggerDisplay("{m_value}", Target = typeof(SQLTypes::SqlDouble))]