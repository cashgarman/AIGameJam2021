/*
Copyright (c) 2021 Omar Duarte
Unauthorized copying of this file, via any medium is strictly prohibited.
Writen by Omar Duarte, 2021.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.Linq;
using UnityEngine;

namespace PluginMaster
{
    public static class BoundsUtils
    {
        private static readonly Vector3 MIN_VECTOR3 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        private static readonly Vector3 MAX_VECTOR3 = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

        public enum ObjectProperty
        {
            BOUNDING_BOX,
            CENTER,
            PIVOT
        }

        public static Bounds GetBounds(Transform transform, ObjectProperty property = ObjectProperty.BOUNDING_BOX)
        {
            var renderer = transform.GetComponent<Renderer>();
            var rectTransform = transform.GetComponent<RectTransform>();

            if (rectTransform == null)
            {
                if (renderer == null || property == ObjectProperty.PIVOT) return new Bounds(transform.position, Vector3.zero);
                if (property == ObjectProperty.CENTER) return new Bounds(renderer.bounds.center, Vector3.zero);
                return renderer.bounds;
            }
            else
            {
                if (property == ObjectProperty.PIVOT) return new Bounds(rectTransform.position, Vector3.zero);
                return new Bounds(rectTransform.TransformPoint(rectTransform.rect.center), rectTransform.TransformVector(rectTransform.rect.size));
            }
        }

        public static Bounds GetBoundsRecursive(Transform transform, bool recursive = true, ObjectProperty property = ObjectProperty.BOUNDING_BOX)
        {
            if (!recursive) return GetBounds(transform, property);
            var children = transform.GetComponentsInChildren<Transform>(true);
            var min = MAX_VECTOR3;
            var max = MIN_VECTOR3;
            var emptyHierarchy = true;
            foreach (var child in children)
            {
                if (child.GetComponent<Renderer>() == null && child.GetComponent<RectTransform>() == null) continue;
                emptyHierarchy = false;
                var bounds = GetBounds(child, property);
                min = Vector3.Min(bounds.min, min);
                max = Vector3.Max(bounds.max, max);
            }
            if (emptyHierarchy) return new Bounds(transform.position, Vector3.zero);
            var size = max - min;
            var center = min + size / 2f;
            return new Bounds(center, size);
        }

        public static Bounds GetSelectionBounds(GameObject[] selection, bool recursive = true, BoundsUtils.ObjectProperty property = BoundsUtils.ObjectProperty.BOUNDING_BOX)
        {
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            foreach (var obj in selection)
            {
                var bounds = GetBoundsRecursive(obj.transform, recursive, property);
                max = Vector3.Max(bounds.max, max);
                min = Vector3.Min(bounds.min, min);
            }
            var size = max - min;
            var center = min + size / 2f;
            return new Bounds(center, size);
        }

        public static Bounds GetBounds(Transform transform, Quaternion rotation)
        {
            var rectTransform = transform.GetComponent<RectTransform>();

            if (rectTransform != null) return new Bounds(rectTransform.TransformPoint(rectTransform.rect.center), rectTransform.TransformVector(rectTransform.rect.size));
            var renderer = transform.GetComponent<Renderer>();
            var meshFilter = transform.GetComponent<MeshFilter>();
            if (renderer == null || meshFilter == null || meshFilter.sharedMesh == null) return new Bounds(transform.position, Vector3.zero);
            var maxSqrDistance = MIN_VECTOR3;
            var minSqrDistance = MAX_VECTOR3;
            var center = GetBounds(transform).center;
            var right = rotation * Vector3.right;
            var up = rotation * Vector3.up;
            var forward = rotation * Vector3.forward;
            foreach (var vertex in meshFilter.sharedMesh.vertices)
            {
                var centerToVertex = transform.TransformPoint(vertex) - center;
                var rightProjection = Vector3.Project(centerToVertex, right);
                var upProjection = Vector3.Project(centerToVertex, up);
                var forwardProjection = Vector3.Project(centerToVertex, forward);
                var rightSqrDistance = rightProjection.sqrMagnitude * (rightProjection.normalized != right ? -1 : 1);
                var upSqrDistance = upProjection.sqrMagnitude * (upProjection.normalized != up ? -1 : 1);
                var forwardSqrDistance = forwardProjection.sqrMagnitude * (forwardProjection.normalized != forward ? -1 : 1);
                maxSqrDistance.x = Mathf.Max(maxSqrDistance.x, rightSqrDistance);
                maxSqrDistance.y = Mathf.Max(maxSqrDistance.y, upSqrDistance);
                maxSqrDistance.z = Mathf.Max(maxSqrDistance.z, forwardSqrDistance);
                minSqrDistance.x = Mathf.Min(minSqrDistance.x, rightSqrDistance);
                minSqrDistance.y = Mathf.Min(minSqrDistance.y, upSqrDistance);
                minSqrDistance.z = Mathf.Min(minSqrDistance.z, forwardSqrDistance);
            }
            var size = new Vector3(
                Mathf.Sqrt(Mathf.Abs(maxSqrDistance.x)) * Mathf.Sign(maxSqrDistance.x) - Mathf.Sqrt(Mathf.Abs(minSqrDistance.x)) * Mathf.Sign(minSqrDistance.x),
                Mathf.Sqrt(Mathf.Abs(maxSqrDistance.y)) * Mathf.Sign(maxSqrDistance.y) - Mathf.Sqrt(Mathf.Abs(minSqrDistance.y)) * Mathf.Sign(minSqrDistance.y),
                Mathf.Sqrt(Mathf.Abs(maxSqrDistance.z)) * Mathf.Sign(maxSqrDistance.z) - Mathf.Sqrt(Mathf.Abs(minSqrDistance.z)) * Mathf.Sign(minSqrDistance.z));
            return new Bounds(center, size);
        }

        public static Bounds GetBoundsRecursive(Transform transform, Quaternion rotation)
        {
            var children = transform.GetComponentsInChildren<Transform>(true);
            var min = MAX_VECTOR3;
            var max = MIN_VECTOR3;
            var emptyHierarchy = true;
            foreach (var child in children)
            {
                if (child.GetComponent<Renderer>() == null && child.GetComponent<RectTransform>() == null) continue;
                emptyHierarchy = false;
                var bounds = GetBounds(child, rotation);
                min = Vector3.Min(bounds.min, min);
                max = Vector3.Max(bounds.max, max);
            }
            if (emptyHierarchy) return new Bounds(transform.position, Vector3.zero);
            var size = max - min;
            var center = min + size / 2f;
            return new Bounds(center, size);
        }

        public static Bounds GetSelectionBounds(GameObject[] selection, Quaternion rotation)
        {
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            foreach (var obj in selection)
            {
                var bounds = GetBoundsRecursive(obj.transform, rotation);
                max = Vector3.Max(bounds.max, max);
                min = Vector3.Min(bounds.min, min);
            }
            var size = max - min;
            var center = min + size / 2f;
            return new Bounds(center, size);
        }

    }
}