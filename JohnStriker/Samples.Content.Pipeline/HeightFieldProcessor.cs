#if !MONOGAME
using System;
using System.ComponentModel;
using DigitalRune.Geometry.Shapes;
using DigitalRune.Graphics.Content.Pipeline;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;


namespace Samples.Content.Pipeline
{
  /// <summary>
  /// Processes a grayscale texture and creates a model with a height field shape.
  /// </summary>
  /// <remarks>
  /// This content processor takes a grayscale textures as input and interprets it as a height map. 
  /// It creates a ModelNode for rendering and a HeightField shape for collision detection. The 
  /// HeightField shape is stored in ModelNode.UserData.
  /// </remarks>
  [ContentProcessor(DisplayName = "HeightField")]
  public class HeightFieldProcessor : ContentProcessor<Texture2DContent, DRModelNodeContent>
  {
    [DefaultValue(10f)]
    [DisplayName("ScaleY")]
    [Description("Scale factor applied to height values.")]
    public virtual float ScaleY { get; set; }

    [DefaultValue(100f)]
    [DisplayName("WidthX")]
    [Description("The height field width along the x-axis.")]
    public virtual float WidthX { get; set; }

    [DefaultValue(100f)]
    [DisplayName("WidthZ")]
    [Description("The height field width along the z-axis.")]
    public virtual float WidthZ { get; set; }

    // The model will be textured with this texture. The filename must be relative to the
    // directory of the height map texture.
    [DefaultValue("Terrain.bmp")]
    [DisplayName("TextureFilename")]
    [Description("The filename of the model texture.")]
    public virtual string TextureFilename { get; set; }


    public HeightFieldProcessor()
    {
      ScaleY = 10;
      WidthX = 100;
      WidthZ = 100;
      TextureFilename = "Terrain.bmp";
    }


    public override DRModelNodeContent Process(Texture2DContent input, ContentProcessorContext context)
    {
      HeightField heightField = CreateHeightFieldFromTexture(input);
      DRModelNodeContent model = CreateModel(input, context, heightField);

      // The content processor returns a ModelNode. The HeightField shape is stored
      // in ModelNode.UserData.
      model.UserData = heightField;

      return model;
    }


    private HeightField CreateHeightFieldFromTexture(Texture2DContent input)
    {
      // Convert the Texture2DContent to PixelBitmapContent which is easier to process.
      input.ConvertBitmapType(typeof(PixelBitmapContent<float>));
      var heightMap = (PixelBitmapContent<float>)input.Mipmaps[0];

      if (heightMap.Width <= 1 || heightMap.Width <= 1)
        throw new Exception("The height map texture must be at least 2 pixels wide.");

      // Create a float[,] array with the scaled pixel values.
      float[,] heights = new float[heightMap.Width, heightMap.Height];
      for (int x = 0; x < heightMap.Width; x++)
        for (int y = 0; y < heightMap.Height; y++)
          heights[x, y] = heightMap.GetPixel(x, y) * ScaleY;

      // Create HeightField shape.
      return new HeightField(WidthX, WidthZ, heights);
    }


    private DRModelNodeContent CreateModel(Texture2DContent input, ContentProcessorContext context, HeightField heightField)
    {
      // We use the XNA MeshBuilder to create the model.
      MeshBuilder meshBuilder = MeshBuilder.StartMesh(input.Name);

      int numberOfHeightValuesInX = heightField.Array.GetLength(0);
      int numberOfHeightValuesInZ = heightField.Array.GetLength(1);
      int numberOfCellsInX = numberOfHeightValuesInX - 1;
      int numberOfCellsInZ = numberOfHeightValuesInZ - 1;

      // Add vertex positions.
      for (int x = 0; x < numberOfHeightValuesInX; x++)
      {
        for (int z = 0; z < numberOfHeightValuesInZ; z++)
        {
          meshBuilder.CreatePosition((float)x / numberOfCellsInX * heightField.WidthX,
                                     heightField.Array[x, z],
                                     (float)z / numberOfCellsInZ * heightField.WidthZ);
        }
      }

      // Set a BasicMaterial for the model.
      var material = new BasicMaterialContent
      {
        SpecularColor = Vector3.Zero,
        Texture = new ExternalReference<TextureContent>(TextureFilename, input.Identity)
      };
      meshBuilder.SetMaterial(material);

      // Add texture coordinates. Each height field cell consists of two triangles.
      int textureChannelId = meshBuilder.CreateVertexChannel<Vector2>(VertexChannelNames.TextureCoordinate(0));
      for (int x = 0; x < numberOfCellsInX; x++)
      {
        for (int z = 0; z < numberOfCellsInZ; z++)
        {
          // First triangle.
          AddVertex(meshBuilder, textureChannelId, x, z, numberOfHeightValuesInX, numberOfHeightValuesInZ);
          AddVertex(meshBuilder, textureChannelId, x + 1, z, numberOfHeightValuesInX, numberOfHeightValuesInZ);
          AddVertex(meshBuilder, textureChannelId, x, z + 1, numberOfHeightValuesInX, numberOfHeightValuesInZ);

          // Second triangle.
          AddVertex(meshBuilder, textureChannelId, x, z + 1, numberOfHeightValuesInX, numberOfHeightValuesInZ);
          AddVertex(meshBuilder, textureChannelId, x + 1, z, numberOfHeightValuesInX, numberOfHeightValuesInZ);
          AddVertex(meshBuilder, textureChannelId, x + 1, z + 1, numberOfHeightValuesInX, numberOfHeightValuesInZ);
        }
      }

      // Create MeshContent. FinishMesh() automatically computes normal vectors.
      MeshContent meshContent = meshBuilder.FinishMesh();

      // Call the DRModelProcessor to convert the MeshContent to DRModelNodeContent.
      DRModelNodeContent model = context.Convert<MeshContent, DRModelNodeContent>(meshContent, "DRModelProcessor");

      return model;
    }


    private void AddVertex(MeshBuilder meshBuilder, int textureChannelId, int x, int z, int numberOfHeightValuesInX, int numberOfHeightValuesInZ)
    {
      // Add texture coordinates.
      Vector2 textureCoord = new Vector2((float)x / (numberOfHeightValuesInX - 1), (float)z / (numberOfHeightValuesInZ - 1));
      meshBuilder.SetVertexChannelData(textureChannelId, textureCoord);

      // Add vertex index.
      meshBuilder.AddTriangleVertex(x * numberOfHeightValuesInX + z);
    }
  }
}
#endif
