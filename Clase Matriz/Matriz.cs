using System; 
public class Matriz 
{
    private double[,] datos; 
    private int filas; 
    private int columnas; 

    public Matriz(int filas, int columnas) 
    { 
        this.filas = filas;
        this.columnas = columnas;
        this.datos = new double[filas, columnas];
    } 

    public int Filas { get { return filas; } } 
    public int Columnas { get { return columnas; } } 
    public double this[int fila, int columna] { get { return datos[fila, columna]; } set { datos[fila, columna] = value; } } 

    public static Matriz Suma (Matriz a, Matriz b) 
    {
        if (a.filas != b.filas || a.columnas != b.columnas) 
        {
            throw new ArgumentException("Las matrices deben tener las mismas dimensiones"); 
        } 

        Matriz sum = new Matriz(a.filas, a.columnas); 
        for (int i = 0; i < a.filas; i++) 
        {
            for (int j = 0; j < a.columnas; j++) 
            {
                sum[i, j] = a[i, j] + b[i, j]; 
            } 
        }
        return sum; 
    } 

    public static Matriz Resta (Matriz a, Matriz b) 
    {
        if (a.filas != b.filas || a.columnas != b.columnas) 
        {
            throw new ArgumentException("Las matrices deben tener las mismas dimensiones"); 
        }

        Matriz res = new Matriz(a.filas, a.columnas); 
        for (int i = 0; i < a.filas; i++) 
        {
            for (int j = 0; j < a.columnas; j++) 
            {
                res[i, j] = a[i, j] - b[i, j]; 
            } 
        } 
        return res; 
    } 

    public static Matriz AxB (Matriz a, Matriz b) 
    {
        if (a.columnas != b.filas) 
        {
            throw new ArgumentException("El número de columnas de la primera matriz debe ser igual al número de filas de la segunda matriz"); 
        } 

        Matriz prod = new Matriz(a.filas, b.columnas); 
        for (int i = 0; i < a.filas; i++) 
        { 
            for (int j = 0; j < b.columnas; j++) 
            { 
                double sum = 0; 
                for (int k = 0; k < a.columnas; k++) 
                { 
                    sum += a[i, k] * b[k, j]; 
                } 
                prod[i, j] = sum; 
            } 
        }
        return prod; 
    } 

    public static Matriz PorEscalar (Matriz a, double k) 
    {
        Matriz prod = new Matriz(a.filas, a.columnas); 
        for (int i = 0; i < a.filas; i++) 
        { 
            for (int j = 0; j < a.columnas; j++) 
            { 
                prod[i, j] = a[i, j] * k; 
            } 
        }
        return prod; 
    } 


    public Matriz Transpuesta() 
    { 
        Matriz trans = new Matriz(columnas, filas); 
        for (int i = 0; i < filas; i++) 
        {
            for (int j = 0; j < columnas; j++) 
            { 
                trans[j, i] = datos[i, j]; 
            } 
        } 
        return trans; 
    } 

    public double Determinante()
    {
        if (filas != columnas)
        {
            throw new ArgumentException("La matriz debe ser cuadrada");
        }

        if (filas == 1)
        {
            return datos[0, 0];
        }

        if (filas == 2)
        {
            return datos[0, 0] * datos[1, 1] - datos[0, 1] * datos[1, 0];
        }

        double det = 0;

        for (int j = 0; j < columnas; j++)
        {
            double[,] menor = new double[filas - 1, columnas - 1];

            for (int i = 1; i < filas; i++)
            {
                for (int k = 0; k < columnas; k++)
                {   
                    if (k < j)
                    {
                        menor[i - 1, k] = datos[i, k];
                    }
                    else if (k > j)
                    {   
                        menor[i - 1, k - 1] = datos[i, k];
                    }
                }
            }

            det += Math.Pow(-1, j) * datos[0, j] * new Matriz(menor).Determinante();
        }

        return det;
    }

}