using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MoogleEngine;

/* 

Clase que contiene toda la información relacionada con los documentos.

Atributos:
    TituloTexto: Diccionario que relaciona el nombre del documento y su contenido sin ningún tipo de procesamiento.
    ID_Title: Diccionario que relaciona el ID del documento con su título.
    Vocabulary: Diccionario que almacena todas las palabras que contiene el total de documentos, que llamaremos vocabulario, 
                a la vez que, por cada palabra, relaciona cada documento en el que aparece con su TF (1 + log(TF)).
    CantDocs: Número total de documentos analizados.
  
*/
public class DataInfo
{
    public Dictionary<string, string[]> TituloTexto { get; private set; }
    // El ID de cada documento lo creé pensando en hacer un operador AND, pero eso ya existe usando dos o más operadores MUST así que está un poco por gusto
    public Dictionary<int, string> ID_Title { get; private set; }
    public Dictionary<string, Dictionary<int, double>> Vocabulary { get; private set; }
    public int CantDocs { get; private set; }

    public DataInfo ()
    {
         // se construyen los diccionarios
         this.TituloTexto = new Dictionary<string, string[]>();
         this.ID_Title = new Dictionary<int, string>();
         this.Vocabulary = new Dictionary<string, Dictionary<int,double>>();
        
        // sourceDirectory es la carpeta donde están todos los documentos
        string sourceDirectory = Path.Join(".." , "Content");
  
        // se cargan los nombres de los documentos y se cuentan
        var txtFiles = Directory.EnumerateFiles(sourceDirectory, "*.txt");
        CantDocs = txtFiles.Count();

                int docID = 1;
                foreach (string currentFile in txtFiles)
                {
                    // por cada documento se carga su contenido y se añade a TituloTexto junto con el título
                    // además, se le asigna un identificador (docID) secuencialmente
                    string fileName = currentFile.Substring(sourceDirectory.Length + 1);
                    fileName = fileName.Substring(0, fileName.Length-4);
                    string[] fileText = File.ReadAllLines(currentFile);
                    this.TituloTexto.Add(fileName, fileText);
                    this.ID_Title.Add(docID, fileName);
                    
                    // se añaden todas las palabras del documento al vocabulario
                    Dictionary<string, int> FileVocabulary = GetFileVocabulary(fileName, fileText);
                    AssignToVocabulary(FileVocabulary, docID);
                    
                    docID++;
                }
           
            // se ordena alfabéticamente el vocabulario
            // cuando lo creé pensé en una utilidad, pero en realidad solo es cómodo para comprobar que funcionan los métodos
            SortVocabulary();

    }

    // método que determina todas las palabras diferentes en un documento y cuántas veces aparecen en él
    private Dictionary<string,int> GetFileVocabulary (string fileName, string[] fileText)
    {
            // se crea un diccionario para relacionar la palabra con su número de ocurrencias
            Dictionary<string,int> FileVocabulary = new Dictionary<string, int>();
            // se crea una lista para que almacene todas las palabras del documento para poder contarlas
            List<string> allWordsInFile = new List<string>();

                    // se procesan y separan todas las palabras del título y se añaden a la lista
                    List<string> allWords = Tokenize(fileName);
                    allWordsInFile.AddRange(allWords);
                    // se hace el mismo proceso por cada línea del documento
                    foreach(string line in fileText)
                    {
                        allWords = Tokenize(line);
                        allWordsInFile.AddRange(allWords);
                    }
                    // se ordena la lista alfabéticamente, solo útil para debug
                    allWordsInFile.Sort();

                    // se añaden las palabras a FileVocabulary y se cuentan las ocurrencias
                    foreach(string word in allWordsInFile)
                    {
                        if(FileVocabulary.ContainsKey(word))
                        {
                            FileVocabulary[word]++;
                        }
                        else
                        {
                            FileVocabulary.Add(word, 1);
                        }
                    }

                    return FileVocabulary;
    }

        
        // método que asigna las palabras diferentes de cada documento al vocabulario
        private void AssignToVocabulary (Dictionary<string,int> FileVocabulary, int docID)
        {
            // por cada palabra, se analiza si ya está en el vocabulario
            // si ya está, se añade al vocabulario la relación documento-TF de la palabra
            // si no está, se añade la palabra al vocabulario y su relación documento-TF
            double TF_Weight;
            foreach(string word in FileVocabulary.Keys)
            {
               if(!this.Vocabulary.ContainsKey(word))
               {
                  Dictionary<int,double> docID_TF = new Dictionary<int, double>();
                  TF_Weight = 1 + Math.Log(FileVocabulary[word]);
                  docID_TF.Add(docID, TF_Weight);
                  this.Vocabulary.Add(word, docID_TF);
               }

               else
               {
                TF_Weight = 1 + Math.Log(FileVocabulary[word]);
                this.Vocabulary[word].Add(docID, TF_Weight);
               }
            }
            
        }

        private void SortVocabulary ()
        {
            List<KeyValuePair<string, Dictionary<int, double>>> list = this.Vocabulary.ToList();
            list.Sort((a, b) => a.Key.CompareTo(b.Key));
            this.Vocabulary.Clear();
            foreach(KeyValuePair<string, Dictionary<int, double>> kvp in list)
            this.Vocabulary.Add(kvp.Key, kvp.Value);
            
        }

        // método que procesa y separa todas las palabras de la línea que recibe para poder compararlas en la búsqueda
        public static List<string> Tokenize (string line, string op = "")
        {
            /*
            Para procesar, toda la línea se pasa a minúsculas con el método de String ToLower, luego se transforman los caracteres
            no alfanuméricos a alfanuméricos y distintos de los operadores utilizando la forma D de la normalización, 
            y finalmente se elimina todo caracter diferente de los alfanuméricos, el espacio y los operadores.
            El string normalizado se separa en palabras
            */
            string lineInProcess = line.ToLower().Normalize(NormalizationForm.FormD);
            lineInProcess = Regex.Replace(lineInProcess,@"[^a-z0-9 "+op+"]+","");
            //pendiente: ñ dedberia cambiarla a nn later
            //pendiente: arreglar errores de tipo amarillo(azul), y/o, 1/5
            List<string> allWords = lineInProcess.Split(' ',StringSplitOptions.RemoveEmptyEntries).ToList();

            return allWords;
        }

}