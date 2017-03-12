using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Amoeba.Test
{
    static class UnlimitedObjectExtensions
    {
        public static dynamic ToUnlimited<T>(this T targetObject)
            where T : class
        {
            return new UnlimitedObject(targetObject);
        }
    }

    class UnlimitedObject : DynamicObject
    {
        private object _targetObject;

        public UnlimitedObject(object targetObject)
        {
            _targetObject = targetObject;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            try
            {
                result = _targetObject.GetType().InvokeMember(binder.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, _targetObject, args);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            try
            {
                result = _targetObject.GetType().InvokeMember(binder.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty, null, _targetObject, new object[] { null });
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            try
            {
                _targetObject.GetType().InvokeMember(binder.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty, null, _targetObject, new object[] { value });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
