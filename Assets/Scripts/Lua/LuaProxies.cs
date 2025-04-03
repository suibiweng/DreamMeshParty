using UnityEngine;
using MoonSharp.Interpreter;

namespace LuaProxies
{
    [MoonSharpUserData]
    public class TransformProxy
    {
        private Transform _transform;

        public TransformProxy(Transform transform)
        {
            _transform = transform;
        }

        public Vector3 GetPosition()
        {
            return _transform.position;
        }

        public void SetPosition(Vector3 position)
        {
            _transform.position = position;
        }

        public Vector3 GetRotation()
        {
            return _transform.eulerAngles;
        }

        public void SetRotation(Vector3 rotation)
        {
            _transform.eulerAngles = rotation;
        }

        public void Translate(Vector3 translation)
        {
            _transform.Translate(translation);
        }

        public void Rotate(Vector3 rotation)
        {
            _transform.Rotate(rotation);
        }

        public Vector3 GetScale()
        {
            return _transform.localScale;
        }

        public void SetScale(Vector3 scale)
        {
            _transform.localScale = scale;
        }
    }

  
    public class GameObjectProxy
    {
        private GameObject _gameObject;

        public GameObjectProxy(GameObject gameObject)
        {
            _gameObject = gameObject;
        }

        public string GetName()
        {
            return _gameObject.name;
        }

        public void SetName(string name)
        {
            _gameObject.name = name;
        }

        public bool IsActive()
        {
            return _gameObject.activeSelf;
        }

        public void SetActive(bool active)
        {
            _gameObject.SetActive(active);
        }
    }
}
