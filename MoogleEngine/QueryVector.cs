namespace MoogleEngine;

/*
Clase que construye un vector de consulta listo para ser comparado con los documentos.

Atributos:
    vector: Diccionario que almacena las palabras y su TF*IDF (TF en la query, IDF en el cuerpo de documentos).
*/
public class QueryVector
{
    public Dictionary<string, double> vector { get; private set; }
    
    public QueryVector (string query, DataInfo data)
    {
       // se crea un diccionario Vocabulary para analizar los valores de IDF de los términos de la query
       Dictionary<string, Dictionary<int, double>> Vocabulary = data.Vocabulary;

       // se procesan y separan todas las palabras de la query y se crea el vector vacío
       List<string> allWordsInQuery = DataInfo.Tokenize(query);
       this.vector = new Dictionary<string, double>();

       // se añade cada palabra de la query al vector y se cuentan sus ocurrencias
       foreach(string word in allWordsInQuery)
       {
           if(!this.vector.ContainsKey(word))
           {
            this.vector.Add(word, 1);
           }
           else
           {
            this.vector[word]++;
           }
       }

       // se calcula el TF*IDF de cada palabra de la query
       foreach(string word in this.vector.Keys)
       {
        // se calcula el peso del TF
        double TF = 1 + Math.Log(this.vector[word]);
        double DF = 0;
        double IDF = 0;
        double TotalDoc = Convert.ToDouble(data.CantDocs);

        if (Vocabulary.ContainsKey(word))
        {
            // se calcula el peso del IDF
            DF = Vocabulary[word].Count();
            IDF = Math.Log(TotalDoc/DF);
        }

        // se calcula el TF*IDF y se asigna a la palabra en el vector
        double TF_IDF = TF*IDF;
        this.vector[word] = TF_IDF;
       }

   }
}