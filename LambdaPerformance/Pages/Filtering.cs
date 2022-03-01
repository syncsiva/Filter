using System.Linq.Expressions;
using System.Dynamic;

namespace LambdaPerformance.Pages
{
    public static class Filtering
    {
        public class WhereFilter
        {
            public string Field { get; set; }

            public bool IgnoreCase { get; set; }

            public bool IgnoreAccent { get; set; }

            public bool IsComplex { get; set; }


            public string Operator { get; set; }

            public string Condition { get; set; }

            public object value { get; set; }

            public List<WhereFilter> predicates { get; set; }

            /// <summary>
            /// Merge the give collection of predicates using And condition.
            /// </summary>
            /// <param name="predicates">List of predicates.</param>
            /// <returns>WhereFilter.</returns>
            public static WhereFilter And(List<WhereFilter> predicates)
            {
                return new WhereFilter() { Condition = "and", IsComplex = true, predicates = predicates };
            }

            /// <summary>
            /// Merge the give collection of predicates using Or condition.
            /// </summary>
            /// <param name="predicates">List of predicates.</param>
            /// <returns>WhereFilter.</returns>
            public static WhereFilter Or(List<WhereFilter> predicates)
            {
                return new WhereFilter() { Condition = "or", IsComplex = true, predicates = predicates };
            }

            /// <summary>
            /// Merge the give predicate using And condition.
            /// </summary>
            /// <param name="fieldName">Specifies the field name.</param>
            /// <param name="operator">Specifies the filter operator.</param>
            /// <param name="value">Specifies the filter value.</param>
            /// <param name="ignoreCase">Performs incasesensitive filtering.</param>
            /// <param name="ignoreAccent">Ignores accent/diacritic letters while filtering.</param>
            /// <returns></returns>
            public WhereFilter And(string fieldName, string @operator = null, object value = null, bool ignoreCase = false, bool ignoreAccent = false)
            {
                WhereFilter predicate = new WhereFilter()
                {
                    Field = fieldName,
                    Operator = @operator,
                    value = value,
                    IgnoreCase = ignoreCase,
                    IgnoreAccent = ignoreAccent
                };
                WhereFilter combined = new WhereFilter()
                {
                    Condition = "and",
                    IsComplex = true,
                    predicates = new List<WhereFilter>()
        {
                    this,
                    predicate
                }
                };
                return combined;
            }

            /// <summary>
            /// Merge the give predicate using And condition.
            /// </summary>
            /// <param name="predicate">Predicate to be merged.</param>
            /// <returns>WhereFilter.</returns>
            public WhereFilter And(WhereFilter predicate)
            {
                WhereFilter combined = new WhereFilter()
                {
                    Condition = "and",
                    IsComplex = true,
                    predicates = new List<WhereFilter>()
        {
                    this,
                    predicate
                }
                };
                return combined;
            }

            /// <summary>
            /// Merge the give predicate using Or condition.
            /// </summary>
            /// <param name="fieldName">Specifies the field name.</param>
            /// <param name="operator">Specifies the filter operator.</param>
            /// <param name="value">Specifies the filter value.</param>
            /// <param name="ignoreCase">Performs incasesensitive filtering.</param>
            /// <param name="ignoreAccent">Ignores accent/diacritic letters while filtering.</param>
            /// <returns></returns>
            public WhereFilter Or(string fieldName, string @operator = null, object value = null, bool ignoreCase = false, bool ignoreAccent = false)
            {
                WhereFilter predicate = new WhereFilter()
                {
                    Field = fieldName,
                    Operator = @operator,
                    value = value,
                    IgnoreCase = ignoreCase,
                    IgnoreAccent = ignoreAccent
                };
                WhereFilter combined = new WhereFilter()
                {
                    Condition = "or",
                    IsComplex = true,
                    predicates = new List<WhereFilter>()
        {
                    this,
                    predicate
                }
                };
                return combined;
            }

            /// <summary>
            /// Merge the give predicate using Or condition.
            /// </summary>
            /// <param name="predicate">Predicate to be merged.</param>
            /// <returns>WhereFilter.</returns>
            public WhereFilter Or(WhereFilter predicate)
            {
                WhereFilter combined = new WhereFilter()
                {
                    Condition = "or",
                    IsComplex = true,
                    predicates = new List<WhereFilter>()
        {
                    this,
                    predicate
                }
                };
                return combined;
            }
        }

    
        public static IEnumerable<T> PerformFiltering<T>(IEnumerable<T> dataSource, List<WhereFilter> whereFilter, string condition)
        {
            return QueryPerformFiltering(dataSource.AsQueryable(), whereFilter, condition);
        }

        public static IQueryable<T> QueryPerformFiltering<T>(IQueryable<T> dataSource, List<WhereFilter> whereFilter, string condition)
        {

            Type type = dataSource.ToList().GetType().GetGenericArguments()[0];
            ParameterExpression paramExpression = Expression.Parameter(type, type?.Name);
            dataSource = dataSource.Where(Expression.Lambda<Func<T, bool>>(PredicateBuilder(dataSource, whereFilter ?? null, condition, paramExpression, type), paramExpression));
            return dataSource;
        }

        private static Expression PredicateBuilder<T>(IQueryable<T> dataSource, List<WhereFilter> whereFilter, string condition, ParameterExpression paramExpression, Type type)
        {
            Type t = typeof(object);
            Expression predicate = null;
            foreach (var filter in whereFilter)
            {
                if (filter.IsComplex)
                {
                    if (predicate == null)
                    {
                        predicate = PredicateBuilder(dataSource, filter.predicates, filter.Condition, paramExpression, type);
                    }
                }
                else
                {
                    string op = filter.Operator == "equal" ? "equals" : filter.Operator == "notequal" ? "notequals" : filter.Operator;
                    FilterType filterType = (FilterType)Enum.Parse(typeof(FilterType), op.ToString(), true);
                    var e = dataSource.GetEnumerator();
                    if (e.MoveNext())
                    {
                        type = e.Current.GetType();
                    }
                    t = type.GetProperty(filter.Field.Split('.')[0]).PropertyType;
                    Type underlyingType = Nullable.GetUnderlyingType(t);
                    var enumValue = new object();

                    if (underlyingType != null)
                    {
                        t = underlyingType;
                    }

                    var value = filter.value;
                    if (value != null)
                    {

                        if (filter.value.GetType().Name == t.Name || filter.value.GetType().Name == "JsonElement")
                        {
                            value = filter.value;
                        }
                    }

                    if (predicate == null)
                    {
                        predicate = Predicate(dataSource, value, filterType, FilterBehavior.StringTyped, !filter.IgnoreCase, type, null, null, paramExpression, filter.Field);
                    }
                    else
                    {
                        if (condition == "or")
                        {
                            predicate = Expression.Or(predicate, Predicate(dataSource, value, filterType, FilterBehavior.StringTyped, !filter.IgnoreCase, type, null, null, paramExpression, filter.Field));
                        }
                        else
                        {
                            predicate = Expression.And(predicate, Predicate(dataSource, value, filterType, FilterBehavior.StringTyped, !filter.IgnoreCase, type, null, null, paramExpression, filter.Field));
                        }
                    }
                }
            }

            return predicate;
        }

        private static Expression Predicate(this IQueryable source, object constValue, FilterType filterType,
                                               FilterBehavior filterBehaviour, bool isCaseSensitive, Type sourceType, Type memberType, Expression memExp, ParameterExpression paramExpression, string propertyName)
        {
            var hasExpressionFunc = false; _ = filterBehaviour;
            string[] propertyNameList = null;
            int propCount = 1;
            if (memExp == null)
            {

                memExp = Expression.PropertyOrField(Expression.Convert(paramExpression, sourceType), propertyName); ;
                memberType = memExp.Type;
                propertyNameList = propertyName.Split('.');
                propCount = propertyNameList.Length;
            }
            else
            {
                hasExpressionFunc = true;
            }

            var value = constValue;
            Expression bExp = null;
            if (memberType == typeof(DateTime?) && value != null && DateTime.TryParse(value.ToString(), out var newdatetime))
            {
                var dateAndTime = newdatetime.TimeOfDay.TotalSeconds;
                var hasVal = Expression.Property(memExp, nameof(Nullable<DateTime>.HasValue));
                var dateVal = Expression.Property(memExp, nameof(Nullable<DateTime>.Value));
                var propertyDate = (dateAndTime == 0) ? Expression.Property(dateVal, nameof(DateTime.Date)) : dateVal;
                memExp = Expression.Condition(Expression.Not(hasVal), Expression.Constant(null, typeof(DateTime?)), Expression.Convert(propertyDate, typeof(DateTime?)));
            }

            if (memberType == typeof(DateTimeOffset?) && value != null && DateTimeOffset.TryParse(value.ToString(), out var newdatetimeoffset))
            {
                var dateAndTime = newdatetimeoffset.TimeOfDay.TotalSeconds;
                var hasVal = Expression.Property(memExp, nameof(Nullable<DateTimeOffset>.HasValue));
                var dateVal = Expression.Property(memExp, nameof(Nullable<DateTimeOffset>.Value));
                var propertyDate = (dateAndTime == 0) ? Expression.Property(dateVal, nameof(DateTimeOffset.Date)) : dateVal;
                memExp = Expression.Condition(Expression.Not(hasVal), Expression.Constant(null, typeof(DateTimeOffset?)), Expression.Convert(propertyDate, typeof(DateTimeOffset?)));
            }

            if (filterType == FilterType.Equals || filterType == FilterType.NotEquals ||
                 filterType == FilterType.LessThan || filterType == FilterType.LessThanOrEqual ||
                 filterType == FilterType.GreaterThan || filterType == FilterType.GreaterThanOrEqual)
            {

                ValueTuple<Expression, Expression, object> v = GetPxExpression(filterType, memberType, value, isCaseSensitive, memExp, bExp);
                memExp = v.Item1;
                bExp = v.Item2;
                value = v.Item3;
            }

            // Coding for complex property
            if (!hasExpressionFunc && propCount > 1)
            {
                Expression basenullexp = null;
                Expression basenotnullexp = null;
                Expression propExp = paramExpression;

                var valueExp = Expression.Constant(value);
                var nullExp = Expression.Constant(null);
                var Exp = Expression.Convert(valueExp, typeof(object));
                var nullvalExp = Expression.Equal(Exp, nullExp);
                var notnullvalExp = Expression.NotEqual(Exp, nullExp);

                for (int prop = 0; prop < propCount - 1; prop++)
                {
                    if (!string.Equals(propExp.Type.Name, "ExpandoObject", StringComparison.Ordinal) && !propExp.Type.IsSubclassOf(typeof(DynamicObject)))
                    {
                        propExp = Expression.PropertyOrField(propExp, propertyNameList[prop]);
                        var tempnullexp = Expression.Equal(propExp, nullExp);
                        if (basenullexp == null)
                        {
                            basenullexp = tempnullexp;
                        }
                        else
                        {
                            basenullexp = Expression.OrElse(basenullexp, tempnullexp);
                        }

                        var tempnotnullexp = Expression.NotEqual(propExp, nullExp);
                        if (basenotnullexp == null)
                        {
                            basenotnullexp = tempnotnullexp;
                        }
                        else
                        {
                            basenotnullexp = Expression.AndAlso(basenotnullexp, tempnotnullexp);
                        }
                    }
                }

                if (basenullexp != null)
                {
                    if (filterType == FilterType.Equals)
                    {
                        basenullexp = Expression.AndAlso(basenullexp, nullvalExp);
                    }
                    else if (filterType == FilterType.NotEquals)
                    {
                        basenullexp = Expression.AndAlso(basenullexp, notnullvalExp);
                    }
                    else if (filterType != FilterType.StartsWith && filterType != FilterType.EndsWith)
                    {
                        bExp = Expression.OrElse(basenullexp, bExp);
                    }

                    bExp = Expression.AndAlso(basenotnullexp, bExp);
                }
            }

            return bExp;
        }

        private static ValueTuple<Expression, Expression, object> GetPxExpression(
                FilterType filterType, Type memberType, object value,
                bool isCaseSensitive, Expression memExp, Expression bExp
                )
        {
            var underlyingType = memberType;
            underlyingType = Nullable.GetUnderlyingType(memberType);

            if (value != null)
            {
                try
                {
                    value = value;
                }
                catch (InvalidCastException)
                {
                }
            }
            var nullablememberType = memberType;

            switch (filterType)
            {
                case FilterType.Equals:
                    if (isCaseSensitive || memberType != typeof(string))
                    {
                        if (value != null)
                        {
                            var exp = Expression.Constant(value, memberType);
#if !EJ2_DNX
                            if ((nullablememberType == memberType && memberType != typeof(object)))
#else
                                 if ((nullablememberType == memberType && memberType != typeof(object)) || memberType.IsEnum)
#endif
                                bExp = Expression.Equal(memExp, Expression.Constant(value, memberType));
                            else
                            {
                                bExp = Expression.Call(exp, exp.Type.GetMethod("Equals", new[] { memExp.Type }), memExp);
                            }
                        }
                        else
                        {
                            memExp = Expression.Convert(memExp, nullablememberType);
                            bExp = Expression.Equal(memExp, Expression.Constant(value, nullablememberType));
                            // bExp = Expression.Call(exp, nullablememberType.GetMethod("Equals", new[] { nullablememberType }), Expression.Constant(memExp));
                        }
                    }

                    break;
            }
            return (memExp, bExp, value);
        }

        public enum FilterType
        {
            /// <summary>
            /// Performs LessThan operation.
            /// </summary>
            LessThan,

            /// <summary>
            /// Performs LessThan Or Equal operation.
            /// </summary>
            LessThanOrEqual,

            /// <summary>
            /// Checks Equals on the operands.
            /// </summary>
            Equals,

            /// <summary>
            /// Checks for Not Equals on the operands.
            /// </summary>
            NotEquals,

            /// <summary>
            /// Checks for Greater Than or Equal on the operands.
            /// </summary>
            GreaterThanOrEqual,

            /// <summary>
            /// Checks for Greater Than on the operands.
            /// </summary>
            GreaterThan,

            /// <summary>
            /// Checks for StartsWith on the string operands.
            /// </summary>
            StartsWith,

            /// <summary>
            /// Checks for EndsWith on the string operands.
            /// </summary>
            EndsWith,

            /// <summary>
            /// Checks for Contains on the string operands.
            /// </summary>
            Contains,

            /// <summary>
            /// Returns invalid type
            /// </summary>
            Undefined,

            /// <summary>
            /// Checks for Between two date on the operands.
            /// </summary>
            Between
        }


        public enum FilterBehavior
        {
            /// <summary>
            /// Parses only StronglyTyped values.
            /// </summary>
            StronglyTyped,

            /// <summary>
            /// Parses all values by converting them as string.
            /// </summary>
            StringTyped
        }
    }
}
