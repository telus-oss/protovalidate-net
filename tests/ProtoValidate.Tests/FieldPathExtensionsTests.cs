// Copyright 2023-2025 TELUS
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Buf.Validate;
using NUnit.Framework;

namespace ProtoValidate.Tests;

[TestFixture]
public class FieldPathExtensionsTests
{
    [Test]
    public void GetPath_WithNullFieldPath_ThrowsArgumentNullException()
    {
        // Arrange
        FieldPath? fieldPath = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => fieldPath!.GetPath());
        Assert.That(exception!.ParamName, Is.EqualTo("fieldPath"));
    }

    [Test]
    public void GetPath_WithEmptyFieldPath_ReturnsEmptyString()
    {
        // Arrange
        var fieldPath = new FieldPath();

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void GetPath_WithSingleFieldName_ReturnsFieldName()
    {
        // Arrange
        var fieldPath = new FieldPath();
        fieldPath.Elements.Add(new FieldPathElement { FieldName = "testField" });

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("testField"));
    }

    [Test]
    public void GetPath_WithMultipleFieldNames_ReturnsDelimitedPath()
    {
        // Arrange
        var fieldPath = new FieldPath();
        fieldPath.Elements.Add(new FieldPathElement { FieldName = "parent" });
        fieldPath.Elements.Add(new FieldPathElement { FieldName = "child" });
        fieldPath.Elements.Add(new FieldPathElement { FieldName = "grandchild" });

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("parent.child.grandchild"));
    }

    [Test]
    public void GetPath_WithFieldNameAndIndex_ReturnsPathWithArrayNotation()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "items" };
        element.Index = 5;
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("items[5]"));
    }

    [Test]
    public void GetPath_WithFieldNameAndZeroIndex_ReturnsFieldNameOnly()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "items" };
        element.Index = 0;
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("items"));
    }

    [Test]
    public void GetPath_WithFieldNameAndBoolKeyTrue_ReturnsPathWithBoolNotation()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "flags" };
        element.BoolKey = true;
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("flags[true]"));
    }

    [Test]
    public void GetPath_WithFieldNameAndBoolKeyFalse_ReturnsPathWithBoolNotation()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "flags" };
        element.BoolKey = false;
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("flags[false]"));
    }

    [Test]
    public void GetPath_WithFieldNameAndIntKey_ReturnsPathWithIntNotation()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "intMap" };
        element.IntKey = 42;
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("intMap[42]"));
    }

    [Test]
    public void GetPath_WithFieldNameAndNegativeIntKey_ReturnsPathWithNegativeIntNotation()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "intMap" };
        element.IntKey = -123;
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("intMap[-123]"));
    }

    [Test]
    public void GetPath_WithFieldNameAndUintKey_ReturnsPathWithUintNotation()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "uintMap" };
        element.UintKey = 123456;
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("uintMap[123456]"));
    }

    [Test]
    public void GetPath_WithFieldNameAndStringKey_ReturnsPathWithStringNotation()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "stringMap" };
        element.StringKey = "key123";
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("stringMap[\"key123\"]"));
    }

    [Test]
    public void GetPath_WithFieldNameAndEmptyStringKey_ReturnsPathWithEmptyStringNotation()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "stringMap" };
        element.StringKey = "";
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("stringMap[\"\"]"));
    }

    [Test]
    public void GetPath_WithStringKeyContainingBackslashes_EscapesBackslashes()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "stringMap" };
        element.StringKey = "path\\to\\file";
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("stringMap[\"path\\\\to\\\\file\"]"));
    }

    [Test]
    public void GetPath_WithStringKeyContainingQuotes_EscapesQuotes()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "stringMap" };
        element.StringKey = "key\"with\"quotes";
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("stringMap[\"key\\\"with\\\"quotes\"]"));
    }

    [Test]
    public void GetPath_WithStringKeyContainingBackslashesAndQuotes_EscapesBoth()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "stringMap" };
        element.StringKey = "path\\\"to\\\"file";
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("stringMap[\"path\\\\\\\"to\\\\\\\"file\"]"));
    }

    [Test]
    public void GetPath_WithComplexNestedPath_ReturnsCorrectPath()
    {
        // Arrange
        var fieldPath = new FieldPath();
        
        // Add parent field
        fieldPath.Elements.Add(new FieldPathElement { FieldName = "parent" });
        
        // Add array element
        var arrayElement = new FieldPathElement { FieldName = "items" };
        arrayElement.Index = 2;
        fieldPath.Elements.Add(arrayElement);
        
        // Add map with string key
        var mapElement = new FieldPathElement { FieldName = "metadata" };
        mapElement.StringKey = "version";
        fieldPath.Elements.Add(mapElement);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("parent.items[2].metadata[\"version\"]"));
    }

    [Test]
    public void GetPath_WithMixedKeyTypes_ReturnsCorrectPath()
    {
        // Arrange
        var fieldPath = new FieldPath();
        
        // Add field with bool key
        var boolElement = new FieldPathElement { FieldName = "boolMap" };
        boolElement.BoolKey = true;
        fieldPath.Elements.Add(boolElement);
        
        // Add field with int key
        var intElement = new FieldPathElement { FieldName = "intMap" };
        intElement.IntKey = -42;
        fieldPath.Elements.Add(intElement);
        
        // Add field with uint key
        var uintElement = new FieldPathElement { FieldName = "uintMap" };
        uintElement.UintKey = 999;
        fieldPath.Elements.Add(uintElement);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("boolMap[true].intMap[-42].uintMap[999]"));
    }

    [Test]
    public void GetPath_WithOnlyFieldNamesNoKeys_ReturnsSimpleDottedPath()
    {
        // Arrange
        var fieldPath = new FieldPath();
        fieldPath.Elements.Add(new FieldPathElement { FieldName = "level1" });
        fieldPath.Elements.Add(new FieldPathElement { FieldName = "level2" });
        fieldPath.Elements.Add(new FieldPathElement { FieldName = "level3" });

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("level1.level2.level3"));
    }

    [Test]
    public void GetPath_WithMultipleArrayIndices_ReturnsPathWithMultipleIndices()
    {
        // Arrange
        var fieldPath = new FieldPath();
        
        var element1 = new FieldPathElement { FieldName = "matrix" };
        element1.Index = 1;
        fieldPath.Elements.Add(element1);
        
        var element2 = new FieldPathElement { FieldName = "row" };
        element2.Index = 3;
        fieldPath.Elements.Add(element2);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("matrix[1].row[3]"));
    }

    [Test]
    public void GetPath_WithLargeUintKey_HandlesLargeNumbers()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "bigMap" };
        element.UintKey = ulong.MaxValue;
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo($"bigMap[{ulong.MaxValue}]"));
    }

    [Test]
    public void GetPath_WithMinIntKey_HandlesMinimumInteger()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "minMap" };
        element.IntKey = long.MinValue;
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo($"minMap[{long.MinValue}]"));
    }

    [Test]
    public void GetPath_WithSpecialCharactersInStringKey_EscapesCorrectly()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "specialMap" };
        element.StringKey = "line1\nline2\ttab\"quote\\backslash";
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        // Only backslashes and quotes should be escaped based on the implementation
        Assert.That(result, Is.EqualTo("specialMap[\"line1\nline2\ttab\\\"quote\\\\backslash\"]"));
    }

    [Test]
    public void GetPath_WithEmptyFieldName_HandlesEmptyFieldName()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "" };
        element.StringKey = "test";
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("[\"test\"]"));
    }

    [Test]
    public void GetPath_WithZeroIndex_DoesNotIncludeIndex()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "array" };
        element.Index = 0; // Zero index should not be included
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("array"));
    }

    [Test]
    public void GetPath_WithNonZeroIndex_IncludesIndex()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "array" };
        element.Index = 1; // Non-zero index should be included
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("array[1]"));
    }

    [Test]
    public void GetPath_WithMaxUintKey_HandlesMaxValue()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "maxMap" };
        element.UintKey = ulong.MaxValue;
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo($"maxMap[{ulong.MaxValue}]"));
    }

    [Test]
    public void GetPath_WithMaxIntKey_HandlesMaxValue()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "maxIntMap" };
        element.IntKey = long.MaxValue;
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo($"maxIntMap[{long.MaxValue}]"));
    }

    [Test]
    public void GetPath_WithZeroIntKey_IncludesZeroIntKey()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "zeroMap" };
        element.IntKey = 0;
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("zeroMap[0]"));
    }

    [Test]
    public void GetPath_WithZeroUintKey_IncludesZeroUintKey()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "zeroUintMap" };
        element.UintKey = 0;
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert
        Assert.That(result, Is.EqualTo("zeroUintMap[0]"));
    }

    [Test]
    public void GetPath_WithComplexStringEscaping_HandlesEscaping()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var element = new FieldPathElement { FieldName = "testMap" };
        element.StringKey = "test\\quote\"end";
        fieldPath.Elements.Add(element);

        // Act
        var result = fieldPath.GetPath();

        // Assert - backslashes and quotes should be escaped
        Assert.That(result, Is.EqualTo("testMap[\"test\\\\quote\\\"end\"]"));
    }

    [Test]
    public void GetPath_WithVeryLongPath_HandlesLongPaths()
    {
        // Arrange
        var fieldPath = new FieldPath();
        var expectedParts = new string[10];
        
        for (int i = 0; i < 10; i++)
        {
            var element = new FieldPathElement { FieldName = $"level{i}" };
            if (i % 2 == 0)
            {
                element.Index = (uint)(i + 1);
                expectedParts[i] = $"level{i}[{i + 1}]";
            }
            else
            {
                expectedParts[i] = $"level{i}";
            }
            fieldPath.Elements.Add(element);
        }

        // Act
        var result = fieldPath.GetPath();

        // Assert
        var expected = string.Join(".", expectedParts);
        Assert.That(result, Is.EqualTo(expected));
    }
}