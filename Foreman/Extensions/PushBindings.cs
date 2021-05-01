namespace Foreman.Extensions
{
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    public class PushBindings
    {
        public static readonly DependencyProperty BindingsProperty =
            DependencyProperty.RegisterAttached(
                "BindingsInternal",
                typeof(PushBindingCollection),
                typeof(PushBindings),
                new UIPropertyMetadata(null));

        public static PushBindingCollection GetBindings(DependencyObject d)
        {
            var bindings = (PushBindingCollection)d.GetValue(BindingsProperty);
            if (bindings == null) {
                d.SetValue(BindingsProperty, new PushBindingCollection(d));
                bindings = (PushBindingCollection)d.GetValue(BindingsProperty);
            }
            return bindings;
        }

        public static void SetBindings(DependencyObject o, PushBindingCollection value)
        {
            o.SetValue(BindingsProperty, value);
        }
    }

    public class PushBindingCollection : FreezableCollection<PushBinding>
    {
        public PushBindingCollection()
        {
        }

        public PushBindingCollection(DependencyObject targetObject)
        {
            TargetObject = targetObject;
            ((INotifyCollectionChanged)this).CollectionChanged += CollectionChanged;
        }

        public DependencyObject TargetObject { get; }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null) {
                foreach (PushBinding binding in e.NewItems)
                    binding.SetupTargetBinding(TargetObject);
            }
        }
    }

    public abstract class FreezableBindingBase : Freezable
    {
        private Binding binding;

        protected Binding Binding => binding ??= new Binding();

        [DefaultValue(null)]
        public object AsyncState
        {
            get => Binding.AsyncState;
            set => Binding.AsyncState = value;
        }

        [DefaultValue(false)]
        public bool BindsDirectlyToSource
        {
            get => Binding.BindsDirectlyToSource;
            set => Binding.BindsDirectlyToSource = value;
        }

        [DefaultValue(null)]
        public IValueConverter Converter
        {
            get => Binding.Converter;
            set => Binding.Converter = value;
        }

        [TypeConverter(typeof(CultureInfoIetfLanguageTagConverter)), DefaultValue(null)]
        public CultureInfo ConverterCulture
        {
            get => Binding.ConverterCulture;
            set => Binding.ConverterCulture = value;
        }

        [DefaultValue(null)]

        public object ConverterParameter
        {
            get => Binding.ConverterParameter;
            set => Binding.ConverterParameter = value;
        }

        [DefaultValue(null)]
        public string ElementName
        {
            get => Binding.ElementName;
            set => Binding.ElementName = value;
        }

        [DefaultValue(null)]
        public object FallbackValue
        {
            get => Binding.FallbackValue;
            set => Binding.FallbackValue = value;
        }

        [DefaultValue(false)]
        public bool IsAsync
        {
            get => Binding.IsAsync;
            set => Binding.IsAsync = value;
        }

        [DefaultValue(BindingMode.Default)]
        public BindingMode Mode
        {
            get => Binding.Mode;
            set => Binding.Mode = value;
        }

        [DefaultValue(false)]
        public bool NotifyOnSourceUpdated
        {
            get => Binding.NotifyOnSourceUpdated;
            set => Binding.NotifyOnSourceUpdated = value;
        }

        [DefaultValue(false)]
        public bool NotifyOnTargetUpdated
        {
            get => Binding.NotifyOnTargetUpdated;
            set => Binding.NotifyOnTargetUpdated = value;
        }

        [DefaultValue(false)]
        public bool NotifyOnValidationError
        {
            get => Binding.NotifyOnValidationError;
            set => Binding.NotifyOnValidationError = value;
        }

        [DefaultValue(null)]
        public PropertyPath Path
        {
            get => Binding.Path;
            set => Binding.Path = value;
        }

        [DefaultValue(null)]
        public RelativeSource RelativeSource
        {
            get => Binding.RelativeSource;
            set => Binding.RelativeSource = value;
        }

        [DefaultValue(null)]
        public object Source
        {
            get => Binding.Source;
            set => Binding.Source = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UpdateSourceExceptionFilterCallback UpdateSourceExceptionFilter
        {
            get => Binding.UpdateSourceExceptionFilter;
            set => Binding.UpdateSourceExceptionFilter = value;
        }

        [DefaultValue(UpdateSourceTrigger.PropertyChanged)]
        public UpdateSourceTrigger UpdateSourceTrigger
        {
            get => Binding.UpdateSourceTrigger;
            set => Binding.UpdateSourceTrigger = value;
        }

        [DefaultValue(false)]
        public bool ValidatesOnDataErrors
        {
            get => Binding.ValidatesOnDataErrors;
            set => Binding.ValidatesOnDataErrors = value;
        }

        [DefaultValue(false)]
        public bool ValidatesOnExceptions
        {
            get => Binding.ValidatesOnExceptions;
            set => Binding.ValidatesOnExceptions = value;
        }

        [DefaultValue(null)]
        public string XPath
        {
            get => Binding.XPath;
            set => Binding.XPath = value;
        }

        [DefaultValue(null)]
        public Collection<ValidationRule> ValidationRules => Binding.ValidationRules;

        protected override void CloneCore(Freezable sourceFreezable)
        {
            var sourceBinding = (FreezableBindingBase)sourceFreezable;
            if (sourceBinding.ElementName != null)
                ElementName = sourceBinding.ElementName;
            else if (sourceBinding.RelativeSource != null)
                RelativeSource = sourceBinding.RelativeSource;
            else if (sourceBinding.Source != null)
                Source = sourceBinding.Source;

            AsyncState = sourceBinding.AsyncState;
            BindsDirectlyToSource = sourceBinding.BindsDirectlyToSource;
            Converter = sourceBinding.Converter;
            ConverterCulture = sourceBinding.ConverterCulture;
            ConverterParameter = sourceBinding.ConverterParameter;
            FallbackValue = sourceBinding.FallbackValue;
            IsAsync = sourceBinding.IsAsync;
            Mode = sourceBinding.Mode;
            NotifyOnSourceUpdated = sourceBinding.NotifyOnSourceUpdated;
            NotifyOnTargetUpdated = sourceBinding.NotifyOnTargetUpdated;
            NotifyOnValidationError = sourceBinding.NotifyOnValidationError;
            Path = sourceBinding.Path;
            UpdateSourceExceptionFilter = sourceBinding.UpdateSourceExceptionFilter;
            UpdateSourceTrigger = sourceBinding.UpdateSourceTrigger;
            ValidatesOnDataErrors = sourceBinding.ValidatesOnDataErrors;
            ValidatesOnExceptions = sourceBinding.ValidatesOnExceptions;
            XPath = sourceBinding.XPath;
            foreach (ValidationRule validationRule in sourceBinding.ValidationRules)
                ValidationRules.Add(validationRule);

            base.CloneCore(sourceFreezable);
        }
    }

    public class PushBinding : FreezableBindingBase
    {
        public PushBinding()
        {
            Mode = BindingMode.OneWayToSource;
        }

        public static readonly DependencyProperty TargetPropertyMirrorProperty =
            DependencyProperty.Register(
                nameof(TargetPropertyMirror),
                typeof(object),
                typeof(PushBinding));

        public object TargetPropertyMirror
        {
            get => GetValue(TargetPropertyMirrorProperty);
            set => SetValue(TargetPropertyMirrorProperty, value);
        }

        public static readonly DependencyProperty TargetPropertyListenerProperty =
            DependencyProperty.Register(
                nameof(TargetPropertyListener),
                typeof(object),
                typeof(PushBinding),
                new UIPropertyMetadata(null, OnTargetPropertyListenerChanged));

        public object TargetPropertyListener
        {
            get => GetValue(TargetPropertyListenerProperty);
            set => SetValue(TargetPropertyListenerProperty, value);
        }

        private static void OnTargetPropertyListenerChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PushBinding)d).TargetPropertyValueChanged();
        }

        [DefaultValue(null)]
        public string TargetProperty { get; set; }

        [DefaultValue(null)]
        public DependencyProperty TargetDependencyProperty { get; set; }

        public void SetupTargetBinding(DependencyObject targetObject)
        {
            if (targetObject == null)
                return;

            // Prevent the designer from reporting exceptions since
            // changes will be made of a Binding in use if it is set
            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            // Bind to the selected TargetProperty, e.g. ActualHeight and get
            // notified about changes in OnTargetPropertyListenerChanged
            var listenerBinding = new Binding {
                Source = targetObject,
                Mode = BindingMode.OneWay,
                Path = TargetDependencyProperty != null
                    ? new PropertyPath(TargetDependencyProperty)
                    : new PropertyPath(TargetProperty)
            };
            BindingOperations.SetBinding(this, TargetPropertyListenerProperty, listenerBinding);

            // Set up a OneWayToSource Binding with the Binding declared in Xaml from
            // the Mirror property of this class. The mirror property will be updated
            // everytime the Listener property gets updated
            BindingOperations.SetBinding(this, TargetPropertyMirrorProperty, Binding);

            TargetPropertyValueChanged();
            if (targetObject is FrameworkElement fe)
                fe.Loaded += (s, e) => TargetPropertyValueChanged();
            else if (targetObject is FrameworkContentElement fce)
                fce.Loaded += (s, e) => TargetPropertyValueChanged();
        }

        private void TargetPropertyValueChanged()
        {
            object value = GetValue(TargetPropertyListenerProperty);
            SetValue(TargetPropertyMirrorProperty, value);
        }

        protected override void CloneCore(Freezable sourceFreezable)
        {
            var binding = (PushBinding)sourceFreezable;
            TargetProperty = binding.TargetProperty;
            TargetDependencyProperty = binding.TargetDependencyProperty;
            base.CloneCore(sourceFreezable);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new PushBinding();
        }
    }
}
