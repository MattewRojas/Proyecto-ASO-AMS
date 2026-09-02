namespace MiniOS.Simulator;

public sealed class SistemaArchivos
{
    public TreeNode CrearArbol()
    {
        var raiz = new TreeNode("Disco local (C:)");
        raiz.Nodes.Add(new TreeNode("Documentos") { Nodes = { "tarea.txt", "reporte.pdf" } });
        raiz.Nodes.Add(new TreeNode("Programas") { Nodes = { "Calculadora.exe", "Editor.exe" } });
        raiz.Nodes.Add(new TreeNode("Sistema") { Nodes = { "kernel.sys", "config.ini" } });
        raiz.Expand();
        return raiz;
    }
}
