using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace netGiant.Intranet.DataLayer
{
    #region Field Section
    [MetadataType(typeof(FieldSectionMetaData))]
    public partial class fieldSection { }

    public class FieldSectionMetaData
    {
        [Required(ErrorMessage = "Field Section Name is required")]
        public string fieldSectionName { get; set; }
        [Required(ErrorMessage = "Sequence number is required")]
        public byte sequenceNo { get; set; }
    }
    #endregion

    #region Field Sub Section
    [MetadataType(typeof(FieldSubSectionMetaData))]
    public partial class fieldSubSection { }

    public class FieldSubSectionMetaData
    {
        [Required(ErrorMessage = "Field Sub Section Name is required")]
        public string fieldSubSectionName { get; set; }
        [Required(ErrorMessage = "Sequence number is required")]
        public byte sequenceNo { get; set; }
        [Required(ErrorMessage = "Field Section is required")]
        public byte fieldSectionFK { get; set; }
    }
    #endregion

    #region Field Type
    [MetadataType(typeof(FieldTypeMetaData))]
    public partial class fieldType { }

    public class FieldTypeMetaData
    {
        [Required(ErrorMessage = "Field Type Name is required")]
        public string fieldTypeName { get; set; }
    }
    #endregion

    #region Field Name
    [MetadataType(typeof(FieldNameMetaData))]
    public partial class fieldName { }
    
    public class FieldNameMetaData
    {
        [Required(ErrorMessage = "Product field name is required")]
        public string fieldName1 { get; set; }
        [Required(ErrorMessage = "Sequence number is required")]
        public byte sequenceNo { get; set; }
        [Required(ErrorMessage = "Product field type is required")]
        public byte fieldTypeFK { get; set; }
        [Required(ErrorMessage = "Product field sub section is required")]
        public byte fieldSubSectionFK { get; set; }
    }
    #endregion
}
