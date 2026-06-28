using UnityEditor;

namespace NaughtyAttributes.Editor
{
    internal class SavedBool
    {
        private bool _value;
        private string _name;

        public bool Value
        {
            get
            {
                return this._value;
            }
            set
            {
                if (this._value == value)
                {
                    return;
                }

                this._value = value;
                EditorPrefs.SetBool(this._name, value);
            }
        }

        public SavedBool(string name, bool value)
        {
            this._name = name;
            this._value = EditorPrefs.GetBool(name, value);
        }
    }
}