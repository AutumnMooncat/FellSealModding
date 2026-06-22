using System.Reflection;
using MelonLoader;

namespace FellSealAssetLoader.Util
{
    public class Context<T> where T : class
    {
        public delegate void HoldDel(T __instance, object[] __args);
        public delegate void HoldReturnDel(T __instance, object[] __args, bool[] __doRun, object[] __result);
        
        public delegate void ReleaseDel(T __instance, object[] __args, object __result);
        public delegate void ReleaseReturnDel(T __instance, object[] __args, object[] __result);
        
        public T instance;
        public object[] args;
        private MethodBase _hook;
        private bool _held;

        public event HoldReturnDel OnHold;
        public event ReleaseReturnDel OnRelease;

        public void Register(MethodBase hook)
        {
            if (IsRegistered())
            {
                Melon<AssetLoaderMod>.Logger.Error($"Context has already registered {_hook}, cannot register {hook}");
                return;
            }

            _hook = hook;
        }

        public bool IsRegistered()
        {
            return _hook != null;
        }
        
        public bool IsRegistered(out MethodBase hook)
        {
            hook = _hook;
            return _hook != null;
        }

        public bool Get()
        {
            return IsRegistered() && _held;
        }

        public bool Get(out T ctx)
        {
            ctx = instance;
            return IsRegistered() && _held;
        }

        public bool Get(out T ctx, out object[] __args)
        {
            ctx = instance;
            __args = args;
            return IsRegistered() && _held;
        }

        public void Hold(T __instance, object[] __args, bool[] __doRunOrig, object[] __result)
        {
            instance = __instance;
            args = __args;
            _held = true;
            OnHold?.Invoke(__instance, __args, __doRunOrig, __result);
        }

        public void Release(T __instance, object[] __args, object[] __result)
        {
            OnRelease?.Invoke(__instance, __args, __result);
            _held = false;
            instance = null;
            args = null;
        }

        public Context<T> WithHold(HoldDel del)
        {
            OnHold += (__instance, __args, orig, result) => del.Invoke(__instance, __args);
            return this;
        }

        public Context<T> WithRelease(ReleaseDel del)
        {
            OnRelease += (__instance, __args, __result) => del.Invoke(__instance, __args, __result[0]);
            return this;
        }
        
        public Context<T> WithHoldReturn(HoldReturnDel del)
        {
            OnHold += del;
            return this;
        }

        public Context<T> WithReleaseReturn(ReleaseReturnDel del)
        {
            OnRelease += del;
            return this;
        }
    }
}