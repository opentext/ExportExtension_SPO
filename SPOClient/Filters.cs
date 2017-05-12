using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using ExportExtensionCommon;

namespace CaptureCenter.SPO
{
    #region SPO List filter
    /// Specity which lists are shown to the user for his selection.
    /// There are three selection criteria
    [Serializable]
    public class SPOListFilter : ModelBase
    {
        #region Sub types
        [Serializable]
        public class BaseType : ModelBase
        {
            private int type;
            public int Type
            {
                get { return type; }
                set { SetField(ref type, value); }
            }
        }

        [Serializable]
        public class TypeTemplateRange : ModelBase
        {
            int from;
            public int From
            {
                get { return from; }
                set { SetField(ref from, value); }
            }
            int to;
            public int To
            {
                get { return to; }
                set { SetField(ref to, value); }
            }
        }
        
        [Serializable]
        public class TitleFilter : ModelBase
        {
            private string pattern;
            public string Pattern
            {
                get { return pattern; }
                set { verifyRegularExpression(value); SetField(ref pattern, value); }
            }
            private bool include;
            public bool Include
            {
                get { return include; }
                set { SetField(ref include, value); }
            }
            private void verifyRegularExpression(string expression)
            {
                try { Regex.Match("SomeText", expression); }  // verify regular expression is valid
                catch (Exception e) { throw new Exception("Illegal expression. Detail: " + e.Message); }
            }
        }

        [Serializable]
        public class ForcedField : ModelBase
        {
            private string fieldTitle;
            public string FieldTitle
            {
                get { return fieldTitle; }
                set { SetField(ref fieldTitle, value); }
            }
        }
        #endregion

        #region Construction
        public SPOListFilter()
        {
            ValidBaseTypes = new ObservableCollection<BaseType>();
            TypeTemplateRanges = new ObservableCollection<TypeTemplateRange>();
            TitleFilters = new ObservableCollection<TitleFilter>();
            ForcedFields = new ObservableCollection<ForcedField>();
        }
        #endregion

        #region Properties
        // 1. Valid base types are base types allowed
        private ObservableCollection<BaseType> validBaseTypes;
        public ObservableCollection<BaseType> ValidBaseTypes
        {
            get { return validBaseTypes; }
            set { SetField(ref validBaseTypes, value); }
        }

        // 2. Template tye filters specify a number range of the lists we allow
        private ObservableCollection<TypeTemplateRange> typeTemplateRanges;
        public ObservableCollection<TypeTemplateRange> TypeTemplateRanges
        {
            get { return typeTemplateRanges; }
            set { SetField(ref typeTemplateRanges, value); }
        }

        // 3. Title filter tell which lists are to show or to be suppressed
        private ObservableCollection<TitleFilter> titleFilters;
        public ObservableCollection<TitleFilter> TitleFilters
        {
            get { return titleFilters; }
            set { SetField(ref titleFilters, value); }
        }

        // 4. Explicit requested fields (Title)
        private ObservableCollection<ForcedField> forcedFields;
        public ObservableCollection<ForcedField> ForcedFields
        {
            get { return forcedFields; }
            set { SetField(ref forcedFields, value); }
        }

        #endregion

        #region Functions
        /// Main function: Return a subset of the input as defined by the filters
        /// This is the logic that is applied:
        ///     * If not ValidBaseTypes are defined, all base types are allowed
        ///     * If ValidBaseTypes are defines, they rule whether a list is allowed
        ///     * If no TemplateTypeRanges are defined, all template types are taken
        ///     * If TemplateTypeRanges are defined, they rule whether a list is allowed
        ///     * Type name filters are applies as they are positive or negative
        public List<SPOList> Filter(List<SPOList> lists)
        {
            List<SPOList> result = new List<SPOList>();
            foreach (SPOList l in lists)
            {
                bool include = true;
                if (ValidBaseTypes.Count > 0 && ValidBaseTypes.Where(n => n.Type == l.BaseType).Count() == 0)
                    include = false;

                if (TypeTemplateRanges.Count > 0)
                {
                    bool found = false;
                    foreach (TypeTemplateRange ttr in TypeTemplateRanges)
                        if (ttr.From <= l.TemplateType && l.TemplateType <= ttr.To)
                        {
                            found = true;
                            break;
                        }
                    if (!found) include = false;
                }

                if (TitleFilters.Count > 0)
                {
                    foreach (TitleFilter tf in TitleFilters)
                    {
                        Regex regex = new Regex(tf.Pattern);
                        Match match = regex.Match(l.Title);
                        if (match.Success)
                        {
                            include = tf.Include;
                            break;
                        }
                    }
                }

                if (include) result.Add(l);
            }
            return result;
        }

        public object Clone()
        {
            return this.MemberwiseClone() as SPOListFilter;
        }
        #endregion
    }
    #endregion
}
