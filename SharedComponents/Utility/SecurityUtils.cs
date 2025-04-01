//------------------------------------------------------------------------------
// <copyright file="SecurityUtils.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

/*
 */


using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

#if WINFORMS_NAMESPACE
    namespace System.Windows.Forms
#elif DRAWING_NAMESPACE
    namespace System.Drawing
#elif WINFORMS_PUBLIC_GRAPHICS_LIBRARY
    namespace System.Internal
#elif SYSTEM_NAMESPACE
    namespace System
#elif SYSTEM_WEB
    namespace System.Web
#elif SYSTEM_DATA_LINQ
    namespace System.Data.Linq
#else

namespace SharedComponents.Utility
#endif
{
    /// <devdoc>
    ///     Useful methods to securely call 'dangerous' managed APIs (especially reflection).
    ///     See http://wiki/default.aspx/Microsoft.Projects.DotNetClient.SecurityConcernsAroundReflection
    ///     for more information specifically about why we need to be careful about reflection invocations.
    /// </devdoc>
    public static class SecurityUtils
    {
        private static volatile ReflectionPermission memberAccessPermission = null;
        private static volatile ReflectionPermission restrictedMemberAccessPermission = null;

        private static ReflectionPermission MemberAccessPermission
        {
            get
            {
                if (memberAccessPermission == null)
                    memberAccessPermission = new ReflectionPermission(ReflectionPermissionFlag.MemberAccess);
                return memberAccessPermission;
            }
        }

        private static ReflectionPermission RestrictedMemberAccessPermission
        {
            get
            {
                if (restrictedMemberAccessPermission == null)
                    restrictedMemberAccessPermission = new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess);
                return restrictedMemberAccessPermission;
            }
        }

        private static void DemandReflectionAccess(Type type)
        {
            try
            {
                MemberAccessPermission.Demand();
            }
            catch (SecurityException)
            {
                DemandGrantSet(type.Assembly);
            }
        }

        [SecuritySafeCritical]
        private static void DemandGrantSet(Assembly assembly)
        {
            var targetGrantSet = assembly.PermissionSet;
            targetGrantSet.AddPermission(RestrictedMemberAccessPermission);
            targetGrantSet.Demand();
        }

        private static bool HasReflectionPermission(Type type)
        {
            try
            {
                DemandReflectionAccess(type);
                return true;
            }
            catch (SecurityException)
            {
            }

            return false;
        }


        /// <devdoc>
        ///     This helper method provides safe access to Activator.CreateInstance.
        ///     NOTE: This overload will work only with public .ctors.
        /// </devdoc>
        internal static object SecureCreateInstance(Type type)
        {
            return SecureCreateInstance(type, null, false);
        }


        /// <devdoc>
        ///     This helper method provides safe access to Activator.CreateInstance.
        ///     Set allowNonPublic to true if you want non public ctors to be used.
        /// </devdoc>
        public static object SecureCreateInstance(Type type, object[] args, bool allowNonPublic)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

            // if it's an internal type, we demand reflection permission.
            if (!type.IsVisible)
                DemandReflectionAccess(type);
            else if (allowNonPublic && !HasReflectionPermission(type))
                allowNonPublic = false;

            if (allowNonPublic)
                flags |= BindingFlags.NonPublic;

            return Activator.CreateInstance(type, flags, null, args, null);
        }

#if (!WINFORMS_NAMESPACE)

        /// <devdoc>
        ///     This helper method provides safe access to Activator.CreateInstance.
        ///     NOTE: This overload will work only with public .ctors.
        /// </devdoc>
        internal static object SecureCreateInstance(Type type, object[] args)
        {
            return SecureCreateInstance(type, args, false);
        }


        /// <devdoc>
        ///     Helper method to safely invoke a .ctor. You should prefer SecureCreateInstance to this.
        ///     Set allowNonPublic to true if you want non public ctors to be used.
        /// </devdoc>
        internal static object SecureConstructorInvoke(Type type, Type[] argTypes, object[] args, bool allowNonPublic)
        {
            return SecureConstructorInvoke(type, argTypes, args, allowNonPublic, BindingFlags.Default);
        }

        /// <devdoc>
        ///     Helper method to safely invoke a .ctor. You should prefer SecureCreateInstance to this.
        ///     Set allowNonPublic to true if you want non public ctors to be used.
        ///     The 'extraFlags' parameter is used to pass in any other flags you need,
        ///     besides Public, NonPublic and Instance.
        /// </devdoc>
        internal static object SecureConstructorInvoke(Type type, Type[] argTypes, object[] args,
            bool allowNonPublic, BindingFlags extraFlags)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            // if it's an internal type, we demand reflection permission.
            if (!type.IsVisible)
                DemandReflectionAccess(type);
            else if (allowNonPublic && !HasReflectionPermission(type))
                allowNonPublic = false;

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | extraFlags;
            if (!allowNonPublic)
                flags &= ~BindingFlags.NonPublic;

            var ctor = type.GetConstructor(flags, null, argTypes, null);
            if (ctor != null)
                return ctor.Invoke(args);

            return null;
        }

        private static bool GenericArgumentsAreVisible(MethodInfo method)
        {
            if (method.IsGenericMethod)
            {
                var parameterTypes = method.GetGenericArguments();
                foreach (var type in parameterTypes)
                    if (!type.IsVisible)
                        return false;
            }
            return true;
        }

        /// <devdoc>
        ///     This helper method provides safe access to FieldInfo's GetValue method.
        /// </devdoc>
        internal static object FieldInfoGetValue(FieldInfo field, object target)
        {
            var type = field.DeclaringType;
            if (type == null)
            {
                // Type is null for Global fields.
                if (!field.IsPublic)
                    DemandGrantSet(field.Module.Assembly);
            }
            else if (!(type != null && type.IsVisible && field.IsPublic))
            {
                DemandReflectionAccess(type);
            }
            return field.GetValue(target);
        }

        /// <devdoc>
        ///     This helper method provides safe access to MethodInfo's Invoke method.
        /// </devdoc>
        internal static object MethodInfoInvoke(MethodInfo method, object target, object[] args)
        {
            var type = method.DeclaringType;
            if (type == null)
            {
                // Type is null for Global methods. In this case we would need to demand grant set on 
                // the containing assembly for internal methods.
                if (!(method.IsPublic && GenericArgumentsAreVisible(method)))
                    DemandGrantSet(method.Module.Assembly);
            }
            else if (!(type.IsVisible && method.IsPublic && GenericArgumentsAreVisible(method)))
            {
                // this demand is required for internal types in system.dll and its friend assemblies. 
                DemandReflectionAccess(type);
            }
            return method.Invoke(target, args);
        }

        /// <devdoc>
        ///     This helper method provides safe access to ConstructorInfo's Invoke method.
        ///     Constructors can't be generic, so we don't check if argument types are visible
        /// </devdoc>
        internal static object ConstructorInfoInvoke(ConstructorInfo ctor, object[] args)
        {
            var type = ctor.DeclaringType;
            if (type != null && !(type.IsVisible && ctor.IsPublic))
                DemandReflectionAccess(type);
            return ctor.Invoke(args);
        }

        /// <devdoc>
        ///     This helper method provides safe access to Array.CreateInstance.
        /// </devdoc>
        internal static object ArrayCreateInstance(Type type, int length)
        {
            if (!type.IsVisible)
                DemandReflectionAccess(type);
            return Array.CreateInstance(type, length);
        }

#endif
    }
}