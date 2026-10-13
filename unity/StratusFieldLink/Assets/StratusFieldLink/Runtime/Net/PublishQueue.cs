using System.Collections.Generic;
using UnityEngine;
using StratusFieldLink.Model;

namespace StratusFieldLink.Net
{
    /// <summary>
    /// Sketches captured while the tablet had no signal. Persisted so closing the
    /// app in a basement does not lose a morning of work.
    ///
    /// Backed by PlayerPrefs, which is fine at field volumes (tens of sketches).
    /// </summary>
    public sealed class PublishQueue
    {
        public const string DefaultStorageKey = "stratus.fieldlink.pendingPublishes";

        private readonly string _storageKey;
        private readonly List<FieldSketchDto> _pending = new List<FieldSketchDto>();

        public PublishQueue(string storageKey = DefaultStorageKey)
        {
            _storageKey = storageKey;
        }

        public int Count
        {
            get { return _pending.Count; }
        }

        public IReadOnlyList<FieldSketchDto> Pending
        {
            get { return _pending; }
        }

        /// <summary>Adds a sketch, replacing any earlier copy with the same id.</summary>
        public void Enqueue(FieldSketchDto sketch)
        {
            if (sketch == null || string.IsNullOrEmpty(sketch.id))
            {
                return;
            }

            _pending.RemoveAll(s => s.id == sketch.id);
            _pending.Add(sketch);
        }

        public FieldSketchDto Peek()
        {
            return _pending.Count == 0 ? null : _pending[0];
        }

        public bool Remove(string id)
        {
            return _pending.RemoveAll(s => s.id == id) > 0;
        }

        public void Clear()
        {
            _pending.Clear();
        }

        public void Save()
        {
            var wrapper = new PendingWrapper { items = _pending };
            PlayerPrefs.SetString(_storageKey, JsonUtility.ToJson(wrapper));
            PlayerPrefs.Save();
        }

        public void Load()
        {
            _pending.Clear();

            var raw = PlayerPrefs.GetString(_storageKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var wrapper = JsonUtility.FromJson<PendingWrapper>(raw);
            if (wrapper != null && wrapper.items != null)
            {
                _pending.AddRange(wrapper.items);
            }
        }

        [System.Serializable]
        private class PendingWrapper
        {
            public List<FieldSketchDto> items = new List<FieldSketchDto>();
        }
    }
}
