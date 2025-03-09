using System;
using Python.Runtime;
using System.Collections.Generic;

public class PythonEmbeddingService
{
    public List<float> GetEmbedding(string text)
    {
        List<float> embeddingResult = new List<float>();

        // Ensure Python.NET is initialized
        if (!PythonEngine.IsInitialized)
        {
            PythonEngine.Initialize();
            PythonEngine.PythonHome = @"C:\Users\Ranoo\AppData\Local\Programs\Python\Python311\";
            PythonEngine.PythonPath = @"C:\Users\Ranoo\AppData\Local\Programs\Python\Python311\python311.dll";
            PythonEngine.Initialize();
            Console.WriteLine("Python initialized successfully!");
        }

        // Run Python code in a single thread safely
        using (Py.GIL()) // Acquire GIL properly
        {
            dynamic embeddingModule = Py.Import("embedding");  // Import Python script
            dynamic embedding = embeddingModule.get_embedding(text);  // Call Python function
            embeddingResult = new List<float>(embedding);  // Convert to C# List
        }

        return embeddingResult;
    }
}
