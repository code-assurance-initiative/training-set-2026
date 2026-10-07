using System.Text;
using System.Xml;
using System.Xml.Linq;
using ReportDesk.Api.Metadata;

namespace ReportDesk.UnitTests.Metadata;

public sealed class MetadataReaderTests
{
    private const string Metadata = """
        <metadata>
          <field name="case">C-2026-118</field>
          <field name="claimant" restricted="true">J. Lind</field>
          <parties>
            <field name="court">District</field>
          </parties>
        </metadata>
        """;

    [Fact]
    public void ImportsTheFieldsOfAMetadataFile()
    {
        var fields = new MetadataImporter().Import(Metadata);
        Assert.Equal(["case", "claimant", "court"], fields.Select(field => field.Name));
    }

    [Fact]
    public void ReadsANamedField()
    {
        Assert.Equal(["C-2026-118"], MetadataFieldReader.ReadField("case", Metadata));
    }

    [Fact]
    public void NeverReturnsARestrictedFieldByName()
    {
        Assert.Empty(MetadataFieldReader.ReadField("claimant", Metadata));
    }

    [Fact]
    public void ReadsTheFieldsOfASection()
    {
        var fields = MetadataSectionReader.ReadSection("parties", Metadata);
        Assert.Equal(new MetadataField("court", "District"), Assert.Single(fields));
    }

    [Theory]
    [InlineData("parties/..")]
    [InlineData("*")]
    [InlineData("a b")]
    public void RefusesASectionThatIsNotAnElementName(string section)
    {
        Assert.Throws<XmlException>(() => MetadataSectionReader.ReadSection(section, Metadata));
    }

    [Fact]
    public void FlattensNestedElementsIntoPaths()
    {
        var root = XElement.Parse("<case><number>7</number><parties><claimant>A</claimant><respondent>B</respondent></parties></case>");
        var fields = MetadataFlattener.Flatten(root);
        Assert.Equal(
            [new("case/number", "7"), new("case/parties/claimant", "A"), new("case/parties/respondent", "B")],
            fields);
    }

    [Fact]
    public void ReadsARetentionSchedule()
    {
        using var xml = new MemoryStream(Encoding.UTF8.GetBytes("<retention><rule class=\"HR\" years=\"7\"/><rule class=\"FIN\" years=\"10\"/></retention>"));
        var rules = RetentionScheduleReader.Read(xml);
        Assert.Equal(7, rules["HR"]);
        Assert.Equal(10, rules["FIN"]);
    }

    [Fact]
    public void RejectsARetentionScheduleWithADocumentType()
    {
        using var xml = new MemoryStream(Encoding.UTF8.GetBytes(
            "<!DOCTYPE retention [<!ENTITY years \"7\">]><retention><rule class=\"HR\" years=\"&years;\"/></retention>"));
        Assert.Throws<XmlException>(() => RetentionScheduleReader.Read(xml));
    }
}
