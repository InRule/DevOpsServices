namespace CheckinRequestListener
{
    
    /// <summary>Specifies the type of selection specified by the implementing attribute.</summary>
    public enum InRuleImportAttributeType
    {
        Import,
        Serializer,
        Available,
        Include,
        IncludeMethods,
        IncludeProperties,
        IncludeFields,
        IncludeBaseMethods,
        RuleWrite,
        RuleWriteAll,
    }
    /// <summary>Base class for InRule import attributes.</summary>
    internal abstract class InRuleImportAttributeBase : System.Attribute
    {
        public abstract InRuleImportAttributeType InRuleImportAttributeType
        {
            get;
        }
    }
    /// <summary>Indicates to the .NET Assembly Schema Importer that the decorated class should be selected.</summary>
    internal class InRuleImportIncludeAttribute : InRuleImportAttributeBase
    {
        public virtual bool Include
        {
            get
            {
                return true;
            }
        }
        public override InRuleImportAttributeType InRuleImportAttributeType
        {
            get
            {
                return InRuleImportAttributeType.Include;
            }
        }
    }
}
