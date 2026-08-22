using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.Utils
{
    /// <summary>
    /// One streamed addressable asset slot, owning the whole handle lifecycle so views
    /// never touch Addressables directly: the newest Load wins (a superseded load is
    /// released without firing its callback), and the handle backing the asset currently
    /// in use is released only after the replacement has been applied — the caller's
    /// onLoaded runs first, so a live texture is never yanked from a renderer. Fully
    /// async by construction; WaitForCompletion is unsupported on WebGL. T is the loaded
    /// shape: Sprite for a single sprite, IList&lt;Sprite&gt; for a sliced sheet's sub-sprites.
    /// </summary>
    public class StreamedAsset<T> where T : class
    {
        private readonly Action<AsyncOperationHandle<T>> _onCompleted;
        private AsyncOperationHandle<T> _current;
        private AsyncOperationHandle<T> _pending;
        private Action<T> _onLoaded;
        private string _debugName;

        public StreamedAsset()
        {
            _onCompleted = OnCompleted;
        }

        /// <summary>
        /// Streams the asset behind <paramref name="runtimeKey"/>; onLoaded fires once it
        /// lands unless a newer Load supersedes this one first. Safe to call again before
        /// the previous load finished. debugName names the content in the failure log.
        /// </summary>
        public void Load(object runtimeKey, Action<T> onLoaded, string debugName)
        {
            if (_pending.IsValid())
            {
                Addressables.Release(_pending);
                _pending = default;
            }
            _onLoaded = onLoaded;
            _debugName = debugName;
            _pending = Addressables.LoadAssetAsync<T>(runtimeKey);
            _pending.Completed += _onCompleted;
        }

        /// <summary>Releases both handles. The owner must stop using the streamed asset
        /// first — after this the backing data may be unloaded.</summary>
        public void Release()
        {
            if (_pending.IsValid())
            {
                Addressables.Release(_pending);
                _pending = default;
            }
            if (_current.IsValid())
            {
                Addressables.Release(_current);
                _current = default;
            }
        }

        private void OnCompleted(AsyncOperationHandle<T> handle)
        {
            if (!handle.Equals(_pending)) return; // superseded by a newer Load

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[Stream] Asset failed to load: " + _debugName);
                Addressables.Release(handle);
                _pending = default;
                return;
            }

            AsyncOperationHandle<T> previous = _current;
            _current = handle;
            _pending = default;
            _onLoaded(handle.Result);
            if (previous.IsValid()) Addressables.Release(previous);
        }
    }
}
