using NSubstitute;
using Railroader.ModManagerInstaller.Abstractions;
using Shouldly;

namespace Railroader.ModManagerInstaller.Tests;

[Collection("ModManagerInstaller")]
public sealed class TestsVdfEntry
{
    [Fact]
    public void Load() {
        // Arrange
        AppServices.File = Substitute.For<IFileStatic>();
        AppServices.File.ReadAllLines("FOO").Returns(["""   "key"    "value"  """]);
        
        // Act
        var entry = VdfEntry.LoadEntry("FOO");
        
        // Assert
        entry.Count.ShouldBe(1);
        entry.ShouldContainKey("key");
        entry["key"].ShouldBe("value");

        AppServices.File.Received().ReadAllLines("FOO");
    }

    [Fact]
    public void Empty()
    {
        // Act
        var result = VdfEntry.Parse([]);
        
        // Assert
        result.Count.ShouldBe(0);
    }
    
    [Fact]
    public void SimpleValue()
    {
        // Arrange
        string[] lines = ["""   "key"    "value"  """];
        
        // Act
        var result = VdfEntry.Parse(lines);
        
        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContainKey("key");
        result["key"].ShouldBe("value");
    }
    
    [Fact]
    public void ManySimpleValues()
    {
        // Arrange
        string[] lines = [
            """   "key1"    "value1" """,
            """ "key2" "value2"   """
        ];
        
        // Act
        var result = VdfEntry.Parse(lines);
        
        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContainKey("key1");
        result["key1"].ShouldBe("value1");
        result.ShouldContainKey("key2");
        result["key2"].ShouldBe("value2");
    }
    
    [Fact]
    public void ComplexEmptyValue()
    {
        // Arrange
        string[] lines = [
            """   "key"   """,
            "{",
         
            "}"
        ];
        
        // Act
        var result = VdfEntry.Parse(lines);
        
        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContainKey("key");
        result["key"].ShouldBeOfType<VdfEntry>().Count.ShouldBe(0);
    }
    
    [Fact]
    public void ComplexValue()
    {
        // Arrange
        string[] lines = [
            """   "key"   """,
            "{",   
            """   "nested"    "value"  """,
            "}"
        ];
        
        // Act
        var result = VdfEntry.Parse(lines);
        
        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContainKey("key");
        var entry = result["key"].ShouldBeOfType<VdfEntry>();
        entry.Count.ShouldBe(1);
        entry.ShouldContainKey("nested");
        entry["nested"].ShouldBe("value");
    }
    
    [Fact]
    public void ComplexNestedEmptyValue()
    {
        // Arrange
        string[] lines = [
            """   "key"   """,
            "{",   
            """   "nested"   """,
            "{", 
            "}",
            "}",
        ];
        
        // Act
        var result = VdfEntry.Parse(lines);
        
        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContainKey("key");
        var entry = result["key"].ShouldBeOfType<VdfEntry>();
        entry.Count.ShouldBe(1);
        entry.ShouldContainKey("nested");
        entry["nested"].ShouldBeOfType<VdfEntry>();
    }
    
    [Fact]
    public void ComplexNestedValue()
    {
        // Arrange
        string[] lines = [
            """   "key"   """,
            "{",   
            """   "nested"   """,
            "{", 
            """   "deep"    "value"  """,
            "}",
            "}",
        ];
        
        // Act
        var result = VdfEntry.Parse(lines);
        
        // Assert
        result.Count.ShouldBe(1);
        result.ShouldContainKey("key");
        var entry = result["key"].ShouldBeOfType<VdfEntry>();
        entry.Count.ShouldBe(1);
        entry.ShouldContainKey("nested");
        var nested = entry["nested"].ShouldBeOfType<VdfEntry>();
        nested.Count.ShouldBe(1);
        nested.ShouldContainKey("deep");
        nested["deep"].ShouldBe("value");
    }
    
    [Fact]
    public void Malformed_NoOpeningBrace()
    {
        // Arrange
        string[] lines = [
            """   "key"   """,
            """   "nested"   """,
        ];
        
        // Act
        var act = () => VdfEntry.Parse(lines);
        
        // Assert
        act.ShouldThrow<VdfException>()
            .Message.ShouldBe("Expected '{' after key");
    }
    
    [Fact]
    public void Malformed_UnknownLine()
    {
        // Arrange
        string[] lines = [
            """   Foo Bar  """,
        ];
        
        // Act
        var act = () => VdfEntry.Parse(lines);
        
        // Assert
        act.ShouldThrow<VdfException>()
            .Message.ShouldBe("Unexpected line in vdf file");
    }
    
    [Fact]
    public void Malformed_MissingClosingBrace()
    {
        // Arrange
        string[] lines = [
            """ "key" """,
            "{",
            """   "nested"    "value"  """
            // no closing }
        ];

        // Act
        var act = () => VdfEntry.Parse(lines);

        // Assert
        act.ShouldThrow<VdfException>()
            .Message.ShouldBe("Unexpected end of vdf file");
    }
}
