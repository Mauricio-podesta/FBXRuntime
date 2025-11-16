using UnityEngine;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using GLTFast;
using NativeFilePickerNamespace;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor; 
#endif

public class RuntimeLoader : MonoBehaviour
{
    private const string GLTF_MIME_TYPES = "model/gltf+json;model/gltf-binary";
    private bool prueba2;
    public void OnPickModelPressed()
    {
#if UNITY_EDITOR
      
        string path = EditorUtility.OpenFilePanel("Seleccionar modelo GLB/GLTF", "", "glb,gltf");
        if (!string.IsNullOrEmpty(path))
        {
            Debug.Log(" Archivo seleccionado en editor: " + path);
            StartCoroutine(ReadAndLoadModel(path));
        }
        else
        {
            Debug.Log(" Selección cancelada.");
        }

#elif UNITY_ANDROID || UNITY_OCULUS
       
        if (!NativeFilePicker.CheckPermission())
        {
            Debug.LogWarning("⚠️ Permiso de almacenamiento no otorgado. El sistema lo pedirá automáticamente.");
        }

        NativeFilePicker.PickFile(
            (path) =>
            {
                if (path != null)
                {
                    Debug.Log(" Archivo seleccionado: " + path);
                    StartCoroutine(ReadAndLoadModel(path));
                }
                else
                {
                    Debug.Log(" Selección cancelada por el usuario.");
                }
            },
            GLTF_MIME_TYPES
        );
#endif
    }

    private IEnumerator ReadAndLoadModel(string uri)
    {
        byte[] modelData = null;

        if (uri.StartsWith("file://"))
        {
            string localPath = uri.Substring("file://".Length);
            if (File.Exists(localPath))
                modelData = File.ReadAllBytes(localPath);
        }
        else if (File.Exists(uri))
        {
            modelData = File.ReadAllBytes(uri);
        }
        else
        {
            using (UnityWebRequest uwr = UnityWebRequest.Get(uri))
            {
                uwr.downloadHandler = new DownloadHandlerBuffer();
                yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                if (uwr.result != UnityWebRequest.Result.Success)
#else
                if (uwr.isNetworkError || uwr.isHttpError)
#endif
                {
                    Debug.LogError(" Error al leer la URI: " + uwr.error);
                    yield break;
                }

                modelData = uwr.downloadHandler.data;
            }
        }

        if (modelData == null || modelData.Length == 0)
        {
            Debug.LogError(" No se pudieron leer los datos del archivo.");
            yield break;
        }

        _ = LoadModelAsync(modelData);
    }

    private async Task LoadModelAsync(byte[] data)
    {
        var gltf = new GltfImport();

        bool success = await gltf.Load(data);
        if (success)
        {
            await gltf.InstantiateMainSceneAsync(transform);
            Debug.Log(" Modelo GLTF/GLB cargado correctamente.");
        }
        else
        {
            Debug.LogError(" Error al importar el modelo.");
        }
    }
}
