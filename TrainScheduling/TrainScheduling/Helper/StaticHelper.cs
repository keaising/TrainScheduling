using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TrainScheduling.Helper
{
    public static class StaticHelper
    {
        /// <summary>
        /// 利用VisualTreeHelper寻找指定依赖对象的父级对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static List<T> FindVisualParent<T>(this DependencyObject obj) where T : DependencyObject
        {
            try
            {
                List<T> TList = new List<T> { };
                DependencyObject parent = VisualTreeHelper.GetParent(obj);
                if (parent != null && parent is T)
                {
                    TList.Add((T)parent);
                    List<T> parentOfParent = FindVisualParent<T>(parent);
                    if (parentOfParent != null)
                    {
                        TList.AddRange(parentOfParent);
                    }
                }
                else
                {
                    List<T> parentOfParent = FindVisualParent<T>(parent);
                    if (parentOfParent != null)
                    {
                        TList.AddRange(parentOfParent);
                    }
                }
                return TList;
            }
            catch (Exception)
            {
                //MessageBox.Show(ee.Message);
                return null;
            }
        }

        /// <summary>
        /// 根据Grid.Name查找Grid，如果没有找到会返回一个新的空白Grid
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name">Grid.Name</param>
        /// <returns></returns>
        public static Grid FindParentGridByName(this DependencyObject obj, string name)
        {
            List<Grid> parentList = obj.FindVisualParent<Grid>();
            var parent = parentList.Find(e => e.Name.ToString() == name);
            if (parent == null)
            {
                return new Grid();
            }
            return parent;
        }

        public static double SumFromTo(this List<double> list, int i, int j)
        {
            double sum = 0;
            for (int k = i; k < j; k++)
                sum += list[k];
            return sum;
        }
    }
}
