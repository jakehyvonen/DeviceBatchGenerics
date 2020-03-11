using System;
using System.Collections.Generic;
using System.Reflection;
using EFDeviceBatchCodeFirst;

namespace DeviceBatchGenerics.Support
{
    /// <summary>
    /// A static class for reflection type functions
    /// </summary>
    public static class Reflection
    {
        /// <summary>
        /// Extension for 'Object' that copies the properties to a destination object.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        public static void CopyLayerProperties(this object source, object destination)
        {
            List<PropertyInfo> ignoredProps = new List<PropertyInfo>() {
            typeof(Layer).GetProperty("LayerId"),
            typeof(Layer).GetProperty("PositionIndex"),
            typeof(Layer).GetProperty("DepositionMethod"),
            typeof(Layer).GetProperty("DepositionMethodId"),
            typeof(Layer).GetProperty("MaterialId"),
            typeof(Layer).GetProperty("SolutionId"),
            typeof(Layer).GetProperty("Device"),
            typeof(Layer).GetProperty("DeviceId"),
            typeof(Layer).GetProperty("Material"),
            typeof(Layer).GetProperty("Solution"),
            typeof(Layer).GetProperty("LayerTemplate"),
            typeof(Layer).GetProperty("Comment")
        };
            // If any this null throw an exception
            if (source == null || destination == null)
                throw new Exception("Source or/and Destination Objects are null");
            // Getting the Types of the objects
            Type typeDest = destination.GetType();
            Type typeSrc = source.GetType();
            // Iterate the Properties of the source instance and  
            // populate them from their desination counterparts  
            var srcProps = new List<PropertyInfo>(typeSrc.GetProperties());
            // remove ignored properties first
            foreach (PropertyInfo p in ignoredProps)
            {
                for (int i = srcProps.Count - 1; i >= 0; i--)
                {
                    if (srcProps[i].Name == p.Name)
                        srcProps.RemoveAt(i);
                }
            }
            foreach (PropertyInfo srcProp in srcProps)
            {

                //Debug.WriteLine("srcProps contains: " + srcProp.Name);
                if (!srcProp.CanRead)
                {
                    continue;
                }
                PropertyInfo targetProperty = typeDest.GetProperty(srcProp.Name);
                if (targetProperty == null)
                {
                    continue;
                }
                if (!targetProperty.CanWrite)
                {
                    continue;
                }
                if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsPrivate)
                {
                    continue;
                }
                if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
                {
                    continue;
                }
                if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                {
                    continue;
                }
                // Passed all tests, lets set the value
                targetProperty.SetValue(destination, srcProp.GetValue(source, null), null);
                //Debug.WriteLine("Copied property: " + targetProperty.Name);
            }
        }
        public static void CopyScanSpecProperties(this object source, object destination)
        {
            List<PropertyInfo> ignoredProps = new List<PropertyInfo>()
            {
                typeof(LJVScanSpec).GetProperty("LJVScanSpecId"),
                typeof(LJVScanSpec).GetProperty("LJVScan")
            };
            // If any this null throw an exception
            if (source == null || destination == null)
                throw new Exception("Source or/and Destination Objects are null");
            // Getting the Types of the objects
            Type typeDest = destination.GetType();
            Type typeSrc = source.GetType();
            // Iterate the Properties of the source instance and  
            // populate them from their desination counterparts  
            var srcProps = new List<PropertyInfo>(typeSrc.GetProperties());
            // remove ignored properties first
            foreach (PropertyInfo p in ignoredProps)
            {
                if (p != null)
                {
                    for (int i = srcProps.Count - 1; i >= 0; i--)
                    {
                        if (srcProps[i].Name == p.Name)
                            srcProps.RemoveAt(i);
                    }
                }
            }
            foreach (PropertyInfo srcProp in srcProps)
            {

                //Debug.WriteLine("srcProps contains: " + srcProp.Name);
                if (!srcProp.CanRead)
                {
                    continue;
                }
                PropertyInfo targetProperty = typeDest.GetProperty(srcProp.Name);
                if (targetProperty == null)
                {
                    continue;
                }
                if (!targetProperty.CanWrite)
                {
                    continue;
                }
                if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsPrivate)
                {
                    continue;
                }
                if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
                {
                    continue;
                }
                if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                {
                    continue;
                }
                // Passed all tests, lets set the value
                targetProperty.SetValue(destination, srcProp.GetValue(source, null), null);
                //Debug.WriteLine("Copied property: " + targetProperty.Name);
            }
        }
    }
}
