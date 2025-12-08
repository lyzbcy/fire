// ModelCompat.cs
// 兼容 Unity.Sentis 和 Unity.Barracuda 的轻量适配层

using UnityEngine;

#if UNITY_BARRACUDA
using Unity.Barracuda;
#endif

#if UNITY_SENTIS
using Unity.Sentis;
#endif

namespace PoseDrive.Runtime.Core
{
    /// <summary>
    /// 模型兼容层：统一 Sentis 和 Barracuda 的 API
    /// 优先使用 Barracuda（如果已安装），否则使用 Sentis
    /// </summary>
#if UNITY_SENTIS && !UNITY_BARRACUDA
    // 仅 Sentis 可用
    using TensorFloat = Unity.Sentis.TensorFloat;
    using ModelAsset = Unity.Sentis.ModelAsset;
    using Model = Unity.Sentis.Model;
    using IWorker = Unity.Sentis.IWorker;
    using TensorShape = Unity.Sentis.TensorShape;

    public static class ModelCompat
    {
        public static Model LoadModel(UnityEngine.Object modelAsset)
        {
            if (modelAsset is ModelAsset sentisAsset)
            {
                return ModelLoader.Load(sentisAsset);
            }
            return null;
        }

        public static Model LoadModel(byte[] onnxBytes)
        {
            return ModelLoader.Load(onnxBytes);
        }

        public static IWorker CreateWorker(Model model, object backend = null)
        {
            // Sentis: 使用 GPUCompute 后端，如果不可用会自动 fallback
            BackendType backendType = BackendType.GPUCompute;
            Model optimized = ModelOptimizer.Optimize(model, backendType);
            return WorkerFactory.CreateWorker(backendType, optimized);
        }

        public static TensorFloat CreateTensorFromTexture(Texture2D texture, int targetWidth, int targetHeight)
        {
            Color[] pixels = texture.GetPixels(0, 0, texture.width, texture.height);
            var shape = new TensorShape(1, targetHeight, targetWidth, 3);
            var tensor = new TensorFloat(shape);

            float scaleX = texture.width / (float)targetWidth;
            float scaleY = texture.height / (float)targetHeight;

            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    int srcX = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(x * scaleX), 0, texture.width - 1);
                    int srcY = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(y * scaleY), 0, texture.height - 1);
                    Color color = pixels[srcY * texture.width + srcX];

                    tensor[0, y, x, 0] = color.r;
                    tensor[0, y, x, 1] = color.g;
                    tensor[0, y, x, 2] = color.b;
                }
            }

            return tensor;
        }

        public static TensorFloat CreateTensorFromFloats(float[] data, int batch = 1, int height = 1, int width = 1, int channels = 1)
        {
            var shape = new TensorShape(batch, height, width, channels);
            var tensor = new TensorFloat(shape);
            int index = 0;
            for (int b = 0; b < batch && index < data.Length; b++)
            {
                for (int h = 0; h < height && index < data.Length; h++)
                {
                    for (int w = 0; w < width && index < data.Length; w++)
                    {
                        for (int c = 0; c < channels && index < data.Length; c++)
                        {
                            tensor[b, h, w, c] = data[index++];
                        }
                    }
                }
            }
            return tensor;
        }
    }

#elif UNITY_BARRACUDA
    // 优先使用 Barracuda
    using TensorFloat = Unity.Barracuda.Tensor;
    using ModelAsset = Unity.Barracuda.NNModel;
    using Model = Unity.Barracuda.Model;
    using IWorker = Unity.Barracuda.IWorker;
    // 注意：Barracuda 不使用 TensorShape 类型，直接使用 Tensor.shape (int[])

    public static class ModelCompat
    {
        public static Model LoadModel(UnityEngine.Object modelAsset)
        {
            if (modelAsset is NNModel barracudaModel)
            {
                return ModelLoader.Load(barracudaModel);
            }
            return null;
        }

        public static Model LoadModel(byte[] onnxBytes)
        {
            return ModelLoader.Load(onnxBytes);
        }

        public static IWorker CreateWorker(Model model, object backend = null)
        {
            // Barracuda 使用 WorkerFactory.Type 而不是 BackendType
            return WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, model);
        }

        public static TensorFloat CreateTensorFromTexture(Texture2D texture, int targetWidth, int targetHeight)
        {
            Color[] pixels = texture.GetPixels(0, 0, texture.width, texture.height);
            var tensor = new Tensor(1, targetHeight, targetWidth, 3);

            float scaleX = texture.width / (float)targetWidth;
            float scaleY = texture.height / (float)targetHeight;

            for (int y = 0; y < targetHeight; y++)
            {
                for (int x = 0; x < targetWidth; x++)
                {
                    int srcX = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(x * scaleX), 0, texture.width - 1);
                    int srcY = UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(y * scaleY), 0, texture.height - 1);
                    Color color = pixels[srcY * texture.width + srcX];

                    tensor[0, y, x, 0] = color.r;
                    tensor[0, y, x, 1] = color.g;
                    tensor[0, y, x, 2] = color.b;
                }
            }

            return tensor;
        }

        public static TensorFloat CreateTensorFromFloats(float[] data, int batch = 1, int height = 1, int width = 1, int channels = 1)
        {
            // Barracuda Tensor 构造：new Tensor(batch, height, width, channels, data)
            return new Tensor(batch, height, width, channels, data);
        }
    }

#else
    // 默认占位（没有安装任何推理引擎）
    public class TensorFloat { }
    public class ModelAsset { }
    public class Model { }
    public interface IWorker { }
    public class TensorShape { }

    public static class ModelCompat
    {
        public static Model LoadModel(UnityEngine.Object modelAsset) { return null; }
        public static Model LoadModel(byte[] onnxBytes) { return null; }
        public static IWorker CreateWorker(Model model, object backend = null) { return null; }
        public static TensorFloat CreateTensorFromTexture(Texture2D texture, int targetWidth, int targetHeight) { return null; }
        public static TensorFloat CreateTensorFromFloats(float[] data, int batch = 1, int height = 1, int width = 1, int channels = 1) { return null; }
    }
#endif
}

