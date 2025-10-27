using UnityEngine;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using GLTFast;
using NativeFilePickerNamespace;
using UnityEngine.Networking;

public class RuntimeLoader : MonoBehaviour
{
    // MIME types separadas por punto y coma.
    private const string GLTF_MIME_TYPES = "model/gltf+json;model/gltf-binary";

    // --- MÉTODOS PÚBLICOS ---

    // Este método se llama desde un botón de la UI
    public void OpenFileBrowserAndLoad()
    {
        // El plugin PickFile es void en esta versión, solo inicia el diálogo.
        NativeFilePicker.PickFile(
            (path) => // Callback: recibe el Content URI o null
            {
                if (path != null)
                {
                    string contentUri = path;
                    Debug.Log("Archivo seleccionado (Content URI): " + contentUri);

                    // Llama al proceso de carga que ahora incluye la lógica de lectura directa
                    StartCoroutine(CopyAndLoadModelCoroutine(contentUri));
                }
                else
                {
                    Debug.Log("❌ Selección de archivo cancelada.");
                }
            },

            // Pasar los MIME types como una ÚNICA cadena de texto.
            GLTF_MIME_TYPES
        );
    }

    // --- MÉTODOS DE LECTURA Y CARGA ---

    // Método actualizado: intenta leer el archivo directamente sin depender de una
    // API de copia que puede no existir. Soporta:
    // - rutas locales (absolute path o file://)
    // - URI remotas o content:// mediante UnityWebRequest
    private IEnumerator CopyAndLoadModelCoroutine(string contentUri)
    {
        byte[] modelData = null;

        // 1) Si es una URI con esquema file:// -> leer con File.ReadAllBytes
        if (contentUri.StartsWith("file://"))
        {
            string localPath = contentUri.Substring("file://".Length);
            if (File.Exists(localPath))
            {
                try
                {
                    modelData = File.ReadAllBytes(localPath);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("❌ Error leyendo archivo local: " + ex.Message);
                    yield break;
                }
            }
        }
        // 2) Si la ruta es directamente accesible como archivo
        else if (File.Exists(contentUri))
        {
            try
            {
                modelData = File.ReadAllBytes(contentUri);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("❌ Error leyendo archivo: " + ex.Message);
                yield break;
            }
        }
        // 3) Intentar leer mediante UnityWebRequest (funciona para muchas URIs, incluidas content:// en varias plataformas)
        else
        {
            using (UnityWebRequest uwr = UnityWebRequest.Get(contentUri))
            {
                uwr.downloadHandler = new DownloadHandlerBuffer();
                yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                if (uwr.result != UnityWebRequest.Result.Success)
#else
                if (uwr.isNetworkError || uwr.isHttpError)
#endif
                {
                    Debug.LogError("❌ Error al leer la URI: " + uwr.error + " | URI: " + contentUri);
                    yield break;
                }

                modelData = uwr.downloadHandler.data;
            }
        }

        // 4) Validar datos leídos
        if (modelData == null || modelData.Length == 0)
        {
            Debug.LogError("❌ Fallo al leer los datos del archivo seleccionado.");
            yield break;
        }

        // 5) Iniciar la importación asíncrona (no bloqueante)
        _ = LoadBinaryModelAsync(modelData);

        yield break;
    }

    // Tu método de carga asíncrona de GLTF/GLB
    async Task LoadBinaryModelAsync(byte[] modelData)
    {
        var gltf = new GltfImport();

        // Se mantiene LoadGltfBinary para evitar más errores, aunque sea obsoleto.
        bool success = await gltf.LoadGltfBinary(modelData);

        if (success)
        {
            // Se usa la versión asíncrona recomendada para Instanciar
            await gltf.InstantiateMainSceneAsync(transform);
            Debug.Log("✅ Modelo 3D cargado exitosamente.");
        }
        else
        {
            Debug.LogError("❌ Fallo al cargar el modelo GLTF/GLB.");
        }
    }
}