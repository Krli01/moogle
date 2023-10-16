using System.Runtime.Intrinsics.Arm;

namespace MoogleEngine;


public static class Moogle
{
    // clase donde se realiza la búsqueda
    public static SearchResult Query(string query, DataInfo data) {
    
        // se crea el vector de todas las palabras de la query
        QueryVector qv_OR = get_OR_vector(query, data);
        
        // se crea, de existir palabras con el operador "!", el vector de dichas palabras
        QueryVector[] qvs_NOT = GetOperationVectors(query, data, "!");

        // se crea, de existir palabras con el operador "^", el vector de dichas palabras
        QueryVector[] qvs_MUST = GetOperationVectors(query, data, "^");

        // diccionario donde se guardarán los vectores de cada documento
        Dictionary<int, Dictionary<string, double>> docVectors;

        // diccionario que contiene los scores de todas las respuestas a devolver
        Dictionary<int, double> OR_results;
        
        if(qv_OR==null)
        {
           // caso en el que todos los términos de la query están negados
           OR_results = GetCorpus(data);
        }
        else
        {
           // se crean y guardan los vectores de las palabras de la query por cada documento
           docVectors = GetDocVectors(data.Vocabulary, qv_OR);

           // se calcula el score de cada documento para devolver los resultados
           OR_results = GetAllScores(qv_OR, docVectors);
        }

        // se crea el arreglo que contiene los resultados asociados a cada palabra negada
        Dictionary<int, double>[] NOT_results = new Dictionary<int, double>[qvs_NOT.Length];
        
        int j = 0;
        foreach(var vector in qvs_NOT)
        {
            // se crean y guardan los vectores de las palabras de la query por cada documento
            docVectors = GetDocVectors(data.Vocabulary, vector);

            // se calcula el score de cada documento para devolver los resultados
            NOT_results[j] = GetAllScores(vector, docVectors);
            j++;
        }

        // se crea el arreglo que contiene los resultados asociados a cada palabra que debe aparecer
        Dictionary<int, double>[] MUST_results = new Dictionary<int, double>[qvs_MUST.Length];
        
        int k = 0;
        foreach(var vector in qvs_MUST)
        {
            // se crean y guardan los vectores de las palabras de la query por cada documento
            docVectors = GetDocVectors(data.Vocabulary, vector);

            // se calcula el score de cada documento para devolver los resultados
            MUST_results[k] = GetAllScores(vector, docVectors);
            k++;
        }
        
        foreach(var vector in NOT_results)
        {
            // se eliminan de los resultados los documentos que contienen las palabras a descartar
            Remove_NOT_results(OR_results, vector); 
        }
        

        foreach(var vector in MUST_results)
        {
            // se eliminan de los resultados aquellos documentos que no contengan las palabras que deben aparecer según el operador "^"
            Get_MUST_results(OR_results, vector); 
        }

        // se cuentan los resultados a devolver
        SearchItem[] items = new SearchItem[OR_results.Count()];

        // se selecciona el snippet a devolver en el resultado y se añade el documento actual a los resultados
        int i=0;
        foreach(int docID in OR_results.Keys)
        {
             string snippet = GetSnippet(docID, qv_OR!, data);
             items[i]=new SearchItem(data.ID_Title[docID], snippet, OR_results[docID]);
            i++;
        }

        //se busca la sugerencia en caso de errores de escritura en la consulta
        string suggestion = GetSuggestion(qv_OR!, data);

        return new SearchResult(items, suggestion);
    }

    // se normaliza el vector dividiendo cada componente entre la norma
    private static void NormalizeVector (Dictionary<string, double> vector)
    {
            // se calcula la norma del vector
            double norm = 0;
            foreach(double componente in vector.Values)
            {
               norm += componente * componente;
            }
            norm = Math.Sqrt(norm);
  
            // se normaliza el vector
            foreach(string word in vector.Keys)
                {
                    vector[word] = vector[word]/norm;
                }
    }

    // método que crea el vector de cada documento según la query
    private static Dictionary<int, Dictionary<string, double>> GetDocVectors (Dictionary<string, Dictionary<int, double>> Vocabulary, QueryVector qv)
    {
        // se crea el diccionario se va a relacionar cada documento con su vector correspondiente
        Dictionary<int, Dictionary<string, double>> docVectors = new Dictionary<int, Dictionary<string, double>>();

        // por cada documento que contenga alguna palabra de la query, se añade la palabra al vector del documento
        foreach(string word in qv.vector.Keys)
        {
            if(Vocabulary.ContainsKey(word))
            {
                foreach(int docID in Vocabulary[word].Keys)
                {
                    if (!docVectors.ContainsKey(docID))
                    {
                        Dictionary<string, double> vector = new Dictionary<string, double>();
                        vector.Add(word, Vocabulary[word][docID]);
                        docVectors.Add(docID, vector);
                    }
                    else
                    {
                        docVectors[docID].Add(word, Vocabulary[word][docID]);
                    }

                }
            }
        }

        /*
        Si la búsqueda consiste en una sola palabra, tanto el vector de la query como el del documento resultará en 1 después de
        la normalización, por lo que no conviene normalizar ambos, ya que la similitud del coseno sería siempre 1 para todos los
        documentos que contengan la palabra.
        Como el vector de la query se normaliza desde su creación, para obtener resultados acertados no se normalizarán los vectores
        de los documentos.
        */
        if (qv.vector.Count()>1)
        {
        foreach(Dictionary<string, double> vector in docVectors.Values)
        {
            NormalizeVector(vector);
        }
        }

        return docVectors;
    }

    // método que calcula el score de todos los documentos
    private static Dictionary<int, double> GetAllScores (QueryVector qv, Dictionary<int, Dictionary<string, double>> docVectors)
    {
        // se crea el diccionario que relaciona los documentos con su score
        Dictionary<int, double> docScore = new Dictionary<int, double>();

        // se calcula el score como la similitud del coseno de los vectores de la query y el documento
        foreach(int docID in docVectors.Keys)
        {
            double score = 0;
            foreach(string word in docVectors[docID].Keys)
            {
                score += docVectors[docID][word] * qv.vector[word];
            }

            docScore.Add(docID, score); 
        }

        // se ordena el diccionario por orden de relevancia para la respuesta
        Dictionary<int, double> sortedScore = docScore.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

        return sortedScore;
    }
    
    //Devuelve el snippet a mostrar por cada documento
    private static string GetSnippet(int docId, QueryVector qv, DataInfo data)
    {
        string titulo = data.ID_Title[docId]; 

        string[] fullText = data.TituloTexto[titulo];

        string snippet = "";
        int maxC = 0;
        int c;
        
        // se busca por cada línea del documento si puede ser un snippet útil y se devuelve el más útil según el criterio del método getSnippetInLine
        foreach (string line in fullText)
        {
            c=GetSnippetInLine(line, qv);
            if (c>maxC)
            {
                snippet = line;
                maxC = c;
            }
        }
        
        /* 
        Puede que no se encuentre un snippet porque las palabras de la búsqueda solo están en el título (e.g. en la búsqueda "García 
        Mátrquez" se devuelven entre los resultados aquellos documentos que contengan solo el texto de obras de García Márquez y cuyo 
        título es de la forma "Gabriel García Márquez - Nombre de la obra"). Para estos casos, se imprime como snippet la primera 
        línea del documento.
         */
        if (snippet=="")
        {
            snippet = fullText[0];
        }

        return snippet;
    }

    // método que determina la relevancia de cada línea para la búsqueda
    // será un snippet más relevante aquella línea que contenga más palabras de la query
    private static int GetSnippetInLine(string line, QueryVector qv)

    {
        List<string> words = DataInfo.Tokenize(line);
        int c=0;
        if(qv!=null)
        {
           foreach(string word in qv.vector.Keys)
           {
               if (words.Contains(word))
               {
                   c++;
               }
           }
        }
        return c;
    }

    // método con el que se crea el vector de la consulta sin operadores
    private static QueryVector get_OR_vector(string query, DataInfo data)
    {
        List<string> words = DataInfo.Tokenize(query, "!");
        List<string> newWords = new List<string>();
        foreach(var word in words)
        {
            // se añaden todas las palabras de la consulta excepto las que deben ser descartadas según el operador "!"
            if(word.Substring(0,1) != "!")
            {
                newWords.Add(word);
            }
        }
        string qv = string.Join(" ", newWords.ToArray());
        
        if(qv!="")
        {
            QueryVector vector = new QueryVector (qv, data);
            NormalizeVector(vector.vector);
            return vector;
        }

        return null!;
    }

    // método con el que se crean los vectores de las operaciones "!" y "^"
    private static QueryVector[] GetOperationVectors(string query, DataInfo data, string op)
    {
        List<string> words = DataInfo.Tokenize(query, op);
        List<string> newWords = new List<string>();
        foreach(var word in words)
        {
            // se añaden solo las palabras marcadas con operadores y se elimiina el operador
            if(word.Substring(0,1)==op)
            {
                newWords.Add(word.Substring(1, word.Length-1));
            }
        }

        QueryVector[] qv = new QueryVector[newWords.Count()];
        
        int i=0;
        foreach(var word in newWords)
        {
           // string goodword = word.Substring(1, word.Length-1);
            qv[i] = new QueryVector (word, data);
            NormalizeVector(qv[i].vector);
            i++;
        }

        return qv;
    }

    // método que saca todos los documentos en caso de que toda la query esté negada
    private static Dictionary<int, double> GetCorpus(DataInfo data)
    {
        Dictionary<int, double> corpus = new Dictionary<int, double>();
        foreach(int docID in data.ID_Title.Keys)
        {
            corpus.Add(docID, 0);
        }
        return corpus; 
    }

     // método donde se elimina la intersección de los resultados de la query con los resultados de los vectores operados con "!"
     private static void Remove_NOT_results(Dictionary<int, double> OR_results, Dictionary<int, double> vector)
     {
         foreach(int docID in vector.Keys)
         {
            if(OR_results.ContainsKey(docID))
            {
                OR_results.Remove(docID);
            }
         }

     }
      // método donde se eliminan de los resultados todas las palabras no contenidas en los vectores operados con "^"
      private static void Get_MUST_results(Dictionary<int, double> OR_results, Dictionary<int, double> vector)
      {
        foreach(int docID in OR_results.Keys)
         {
            if(!vector.ContainsKey(docID))
            {
                OR_results.Remove(docID);
            }
         }
      }
    // método para obtener la sugerencia de busqueda
    private static string GetSuggestion(QueryVector query, DataInfo data)
    {
        string querystring = "";
        string suggestion = "";

        foreach (var word in query.vector.Keys)
        {
            querystring = querystring+word+" ";
            if (data.Vocabulary.ContainsKey(word))
            {
                suggestion = suggestion+word+" ";
                continue;
            }
           
            int MinimumEditDistance = int.MaxValue;
            string PossibleSuggestion = "";
            
            foreach (var term in data.Vocabulary.Keys)
            {
                int EditDistance = GetLevenshteinDistance(word, term);
                
                if (EditDistance == 1) 
                    {
                    PossibleSuggestion = term;
                    break;
                    }
                if (EditDistance < MinimumEditDistance)
                    {
                    MinimumEditDistance = EditDistance;
                    PossibleSuggestion = term;
                    }
            }

            suggestion = suggestion+PossibleSuggestion+" ";
        }

        querystring = querystring.Trim();
        suggestion = suggestion.Trim();

        if(querystring!=suggestion)return suggestion;
        return "";
        //Desventaja de esta sugerencia: el vocabulario está conformado por cualquier término contenido en el cuerpo de documentos, sea considerado una palabra válida o no.
        //Esto implica que la sugerencia puede no resultar la más exacta en algunos casos, más allá del funcionamiento del algoritmo.
    }
    
    // método para obtener la distancia mínima de edición
    private static int GetLevenshteinDistance(string a, string b)
    {
        //odiamos la recursión
        int n = a.Length;
        int m = b.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0)
        {
            return m;
        }

        if (m == 0)
        {
            return n;
        }

        for (int i = 0; i <= n; d[i, 0] = i++)
        {
        }

        for (int j = 0; j <= m; d[0, j] = j++)
        {
        }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (b[j - 1] == a[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];

    }

}
