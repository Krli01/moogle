# Moogle!

Proyecto de Programación I. 
Karla Díaz Saura. 
Grupo C113. 
Curso 2023.

## 
El presente proyecto consiste en un sistema de recuperación de la información básico, construido con lenguaje C# y el framework .NET, que implementa una búsqueda avanzada basada en el modelo vectorial de recuperación de la información en una colección de documentos de texto. Al ejecutar la búsqueda, el programa devuelve resultados ordenados según su relevancia a la vez que, por cada documento, proporciona un fragmento del texto de importancia para el usuario según su consulta.

## Requisitos
.NET Framework 7.0

## Funcionalidades
El programa se puede utilizar para realizar búsquedas libres, así como para encontrar resultados más específicos siguiendo una sintaxis especial. Lo último se puede lograr mediante el empleo de los siguientes operadores:
   - No aparición (!):
       Ejemplo de uso: !término
       El término afectado por el operador '!' no deberá aparecer en ninguno de los documentos que se devuelvan como resultado.
   - Aparición requerida (^):
       Ejemplo de uso: ^término
       El término afectado por el operador '^' deberá aparecer en todos los documentos que se devuelvan como resultado.


## Ejecución
En primer lugar se debe añadir los documentos (con extensión '.txt') que se desea analizar a la carpeta Content.
Para iniciar el programa, se debe ejecutar en la carpeta raíz del proyecto uno de los siguientes comandos, en dependencia del sistema operativo con que cuente el usuario:
   - Linux: make dev
   - Windows: dotnet watch run --project MoogleServer
