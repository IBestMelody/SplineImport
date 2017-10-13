
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using System.IO;

[RequireComponent(typeof(LineRenderer))]
public class SplineImport : MonoBehaviour
{
    public GeoData[] dataList;
    public int _selectId = -1;
    public float _unitScale = 1.0f;

#if UNITY_EDITOR
    /// <summary>
    /// read dae file
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public bool readDAE( string filePath )
    {
        try
        {
            FileInfo f = new FileInfo(filePath);
            StreamReader sr = f.OpenText();
            string xmldata = sr.ReadToEnd();
            sr.Close();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmldata);
            if (doc.ChildNodes.Count > 1) return ParseDocument(doc);
        }
        catch
        {
            return false;
        }
        
        return true;
    }
#endif

    //parse xml document
    public bool ParseDocument( XmlDocument doc )
    {
        if (doc.ChildNodes[1].Name != "COLLADA" && doc.ChildNodes[1].Name != "collada") return false;
        XmlNode assetNode = doc.ChildNodes[1].ChildNodes[0];
        dataList = null;
        string toolinfo = AbsDAEFormatParser.findNodeInDepth("authoring_tool", assetNode).InnerText;
        switch( ToolNameParse(toolinfo) )
        {
            case "c4d":
                dataList = new C4DDAEFormatParser().Parse(doc.ChildNodes[1]);
                break;
        }

        if (dataList == null) return false;
        _selectId = -1;
        return true;
    }

    string ToolNameParse( string info )
    {
        if (info.IndexOf("CINEMA4D") > -1) return "c4d";
        if (info.IndexOf("cinema4d") > -1) return "c4d";
        return "";
    }

    /// <summary>
    /// create spline 
    /// </summary>
    public void CreateLine()
    {
        if (dataList == null) return;
        if (dataList.Length == 0) return;
        LineRenderer lr = GetComponent<LineRenderer>();
        int l = dataList[_selectId].points.Length;
        lr.positionCount = l;
        Vector3[] positions = new Vector3[l];
        for (int i = 0; i < l; i++) positions[i] = dataList[_selectId].points[i] * _unitScale;
        lr.SetPositions(positions);
    }
}

[System.Serializable]
public class GeoData
{
    public string id;
    public string name;
    public Vector3[] points;

    public void setData( string data )
    {
        string[] values = data.Split(' ');
        if (values.Length < 3) throw new System.Exception("Import error.");
        points = new Vector3[(int)(values.Length / 3)];
        for ( int i = 0; i < points.Length; i++ )
        {
            var v3 = new Vector3();
            v3.x = float.Parse(values[i * 3]);
            v3.y = float.Parse(values[i * 3 + 1]);
            v3.z = float.Parse(values[i * 3 + 2]);
            points[i] = v3;
        }
    }
}

public class C4DDAEFormatParser:AbsDAEFormatParser
{
    public override GeoData[] Parse(XmlNode colladaNode )
    {
        XmlNode geoNode = findNode("library_geometries", colladaNode);
        XmlNode objNode = findNode("library_visual_scenes", colladaNode);
        if (geoNode == null || objNode == null) return null;
        if (geoNode.ChildNodes.Count == 0 || objNode.ChildNodes.Count == 0) return null;

        List<GeoData> gd = new List<GeoData>();

        //get geometry vertices 
        for (int i = 0; i < geoNode.ChildNodes.Count; i++)
        {
            var nd = geoNode.ChildNodes[i];
            if (findNodeInDepth("linestrips", nd) == null ) continue;
            var geo = new GeoData();
            geo.id = findAttributeValue("id", nd);
            XmlNode pointsNode = findNodeInDepth("float_array", nd);
            if (pointsNode != null)
            {
                geo.setData(pointsNode.InnerText);
                gd.Add(geo);
            }
        }

        XmlNode sceneNode = objNode.ChildNodes[0];

        //get spline object name
        List<XmlNode> checklist = findNodeList("node", sceneNode);
        if (checklist.Count == 0) return null;
        XmlNode cnd;
        while( checklist.Count > 0 )
        {
            cnd = checklist[0];
            var geoIdNode = findNode("instance_geometry", cnd);
            if( geoIdNode != null )
            {
                var id = findAttributeValue("url", geoIdNode).Substring(1);
                foreach ( GeoData g in gd )
                {
                    if( g.id == id )
                    {
                        g.name = findAttributeValue("name", cnd);
                        break;
                    }
                }
            }
            var ndlist = findNodeList("node", cnd);
            foreach (XmlNode xn in ndlist) checklist.Add(xn);
            checklist.RemoveAt(0);
        }

        return gd.ToArray();
    }
}

public abstract class AbsDAEFormatParser
{
    virtual public GeoData[] Parse(XmlNode colladaNode)
    {
        return null;
    }

    public static XmlNode findNode(string name, XmlNode node)
    {
        foreach (XmlNode n in node.ChildNodes)
        {
            if (n.Name == name) return n;
        }
        return null;
    }

    public static List<XmlNode> findNodeList(string name, XmlNode node)
    {
        List<XmlNode> ls = new List<XmlNode>();
        foreach (XmlNode n in node.ChildNodes)
        {
            if (n.Name == name) ls.Add(n);
        }
        return ls;
    }

    public static XmlNode findNodeInDepth(string name, XmlNode node)
    {
        List<XmlNode> checkList = new List<XmlNode>();
        foreach (XmlNode n in node.ChildNodes)
        {
            checkList.Add(n);
        }
        XmlNode nd;
        while (checkList.Count > 0)
        {
            nd = checkList[0];
            if (nd.Name == name) return nd;
            if (nd.ChildNodes.Count > 0)
            {
                foreach (XmlNode xn in nd.ChildNodes) checkList.Add(xn);
            }
            checkList.RemoveAt(0);
        }
        return null;
    }

    public static string findAttributeValue(string name, XmlNode node)
    {
        if (node.Attributes != null)
        {
            foreach (XmlAttribute a in node.Attributes)
            {
                if (a.Name == name) return a.Value;
            }
        }
        return null;
    }
}

