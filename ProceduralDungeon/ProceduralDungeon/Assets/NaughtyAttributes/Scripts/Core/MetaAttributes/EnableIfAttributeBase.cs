using System;

namespace NaughtyAttributes
{
    public abstract class EnableIfAttributeBase : MetaAttribute
    {
        public string[] Conditions { get; private set; }
        public EConditionOperator ConditionOperator { get; private set; }
        public bool Inverted { get; protected set; }

        /// <summary>
        ///		If this not null, <see cref="Conditions"/>[0] is name of an enum variable.
        /// </summary>
        public Enum EnumValue { get; private set; }

        public EnableIfAttributeBase(string condition)
        {
            this.ConditionOperator = EConditionOperator.And;
            this.Conditions = new string[1] { condition };
        }

        public EnableIfAttributeBase(EConditionOperator conditionOperator, params string[] conditions)
        {
            this.ConditionOperator = conditionOperator;
            this.Conditions = conditions;
        }

        public EnableIfAttributeBase(string enumName, Enum enumValue)
            : this(enumName)
        {
            if (enumValue == null)
            {
                throw new ArgumentNullException(nameof(enumValue), "This parameter must be an enum value.");
            }

            this.EnumValue = enumValue;
        }
    }
}
