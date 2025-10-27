using UnityEngine;
using SFB;
using System.Collections;
using System.IO;
using System.Threading.Tasks; // Necesario para la carga asíncrona
using GLTFast; // Necesario para el importador GLTF (glTFast)

public class RuntimeLoader : MonoBehaviour
{
    // Este método se llama desde un botón de la UI
    public void OpenFileBrowserAndLoad()
    {
        // 1. Definir los filtros de extensión
        ExtensionFilter[] extensions = new[] {
            new ExtensionFilter("Modelos GLB/GLTF", "glb", "gltf"),
        };

        // 2. Abrir el cuadro de diálogo de selección de archivos
        // La variable 'paths' se declara AQUÍ
        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Seleccionar Modelo 3D",
            "",
            extensions,
            false
        );

        // 3. Verificar si el usuario seleccionó un archivo
        // Ahora 'paths' es accesible porque está en el mismo ámbito.
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0])) // <--- Estos son los puntos donde te daba error
        {
            string localPath = paths[0];
            Debug.Log("Archivo seleccionado: " + localPath);

            // 4. Iniciar la lectura y la importación (usando el método asíncrono)
            LoadLocalModelAsync(localPath);
        }
    }

    // Usamos async Task para la carga, que es el método recomendado para glTFast
    async Task LoadLocalModelAsync(string localPath)
    {
        // 1. Leer el archivo binario completo del disco
        byte[] modelData = File.ReadAllBytes(localPath);

        // 2. Crear una nueva instancia del importador GLTF
        var gltf = new GltfImport();

        // 3. Cargar los datos binarios de forma asíncrona usando 'await'
        bool success = await gltf.LoadGltfBinary(modelData);

        if (success)
        {
            // 4. Instanciar el modelo en la escena.
            gltf.InstantiateMainScene(transform);
            Debug.Log("✅ Modelo 3D cargado exitosamente desde la ruta local.");
        }
        else
        {
            Debug.LogError("❌ Fallo al cargar el modelo GLTF/GLB desde el disco.");
        }
    }
}