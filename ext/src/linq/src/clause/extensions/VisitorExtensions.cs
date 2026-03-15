using mooSQL.data.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mooSQL.linq.SqlQuery
{
    public static class VisitorExtensions
    {
        public static T[]? VisitElements<T>(this T[]? arr1, VisitMode mode, Func<T, Clause> Visit)
    where T : class, ISQLNode
        {
            if (arr1 == null)
                return null;

            switch (mode)
            {
                case VisitMode.ReadOnly:
                    {
                        foreach (var t in arr1)
                        {
                            _ = Visit(t);
                        }

                        return arr1;
                    }

                case VisitMode.Modify:
                    {
                        for (var i = 0; i < arr1.Length; i++)
                        {
                            arr1[i] = Visit(arr1[i]) as T;
                        }

                        return arr1;
                    }

                case VisitMode.Transform:
                    {
                        T[]? arr2 = null;

                        for (var i = 0; i < arr1.Length; i++)
                        {
                            var elem1 = arr1[i];
                            var elem2 = Visit(elem1) as T;

                            if (!ReferenceEquals(elem1, elem2))
                            {
                                if (arr2 == null)
                                {
                                    arr2 = new T[arr1.Length];

                                    for (var j = 0; j < i; j++)
                                        arr2[j] = arr1[j];
                                }

                                arr2[i] = elem2;
                            }
                            else if (arr2 != null)
                                arr2[i] = elem1;
                        }

                        return arr2 ?? arr1;
                    }
                default:
                    throw new InvalidDataException();
            }
        }

        public static List<T>? VisitElements<T>(this List<T>? list1, VisitMode mode,Func<T, Clause> Visit)
    where T : class, ISQLNode
        {
            if (list1 == null)
                return null;

            switch (mode)
            {
                case VisitMode.ReadOnly:
                    {
                        foreach (var t in list1)
                        {
                            _ = Visit(t);
                        }

                        return list1;
                    }

                case VisitMode.Modify:
                    {
                        for (var i = 0; i < list1.Count; i++)
                        {
                            list1[i] = Visit(list1[i]) as T;
                        }

                        return list1;
                    }

                case VisitMode.Transform:
                    {
                        List<T>? list2 = null;

                        for (var i = 0; i < list1.Count; i++)
                        {
                            var elem1 = list1[i];
                            var elem2 = Visit(elem1) as T;

                            if (!ReferenceEquals(elem1, elem2))
                            {
                                if (list2 == null)
                                {
                                    list2 = new List<T>(list1.Count);

                                    for (var j = 0; j < i; j++)
                                        list2.Add(list1[j]);
                                }

                                list2.Add(elem2);
                            }
                            else if (list2 != null)
                                list2.Add(elem1);
                        }

                        return list2 ?? list1;
                    }

                default:
                    throw new InvalidDataException();
            }
        }

        public static List<T>? VisitElements<T>(this List<T>? list1, VisitMode mode, Func<T, T> transformFunc)
    where T : class
        {
            if (list1 == null)
                return null;

            switch (mode)
            {
                case VisitMode.ReadOnly:
                    {
                        foreach (var t in list1)
                        {
                            _ = transformFunc(t);
                        }

                        return list1;
                    }

                case VisitMode.Modify:
                    {
                        for (var i = 0; i < list1.Count; i++)
                        {
                            list1[i] = transformFunc(list1[i]);
                        }

                        return list1;
                    }

                case VisitMode.Transform:
                    {
                        List<T>? list2 = null;

                        for (var i = 0; i < list1.Count; i++)
                        {
                            var elem1 = list1[i];
                            var elem2 = transformFunc(elem1);

                            if (!ReferenceEquals(elem1, elem2))
                            {
                                if (list2 == null)
                                {
                                    list2 = new List<T>(list1.Count);

                                    for (var j = 0; j < i; j++)
                                        list2.Add(list1[j]);
                                }

                                list2.Add(elem2);
                            }
                            else if (list2 != null)
                                list2.Add(elem1);
                        }

                        return list2 ?? list1;
                    }

                default:
                    throw new InvalidDataException();
            }
        }

        public static List<T[]>? VisitListOfArrays<T>(this List<T[]>? list1, VisitMode mode, Func<T, Clause> Visit)
    where T : class, ISQLNode
        {
            if (list1 == null)
                return null;

            switch (mode)
            {
                case VisitMode.ReadOnly:
                    {
                        foreach (var t in list1)
                        {
                            _ = VisitElements(t, VisitMode.ReadOnly,Visit);
                        }

                        return list1;
                    }
                case VisitMode.Modify:
                    {
                        for (var i = 0; i < list1.Count; i++)
                        {
                            list1[i] = VisitElements(list1[i], VisitMode.Modify, Visit);
                        }

                        return list1;
                    }
                case VisitMode.Transform:
                    {
                        List<T[]>? list2 = null;

                        for (var i = 0; i < list1.Count; i++)
                        {
                            var elem1 = list1[i];
                            var elem2 = VisitElements(elem1, VisitMode.Transform, Visit);

                            if (elem1 != elem2)
                            {
                                if (list2 == null)
                                {
                                    list2 = new List<T[]>(list1.Count);

                                    for (var j = 0; j < i; j++)
                                    {
                                        list2.Add(list1[j].ToArray());
                                    }
                                }

                                list2.Add(elem2);
                            }
                            else if (list2 != null)
                            {
                                list2.Add(elem1);
                            }
                        }

                        return list2 ?? list1;
                    }

                default:
                    throw new InvalidDataException();
            }
        }

    }
}
