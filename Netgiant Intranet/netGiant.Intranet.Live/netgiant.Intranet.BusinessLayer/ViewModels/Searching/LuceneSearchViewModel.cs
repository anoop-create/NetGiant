using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using netGiant.Intranet.DataLayer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace netGiant.Intranet.BusinessLayer.ViewModels.Searching
{
    public class LuceneSearchViewModel
    {
        public LuceneSearchViewModel()
        {
            ProductResults = new List<string>();
            EquipResults = new List<string>();
        }

        public string SearchTerm { get; set; }
        public List<string> ProductResults { get; set; }
        public List<string> EquipResults { get; set; }
        public string TimeTaken { get; set; }

        public LuceneSearchViewModel CreateIndex()
        {
            using (ngmdEntities db = new ngmdEntities())
            {
                var prods = db.product.Include(x => x.AxisFields)
                    .OrderBy(x => x.productID)
                    .Where(x => x.websiteInventory.Any(y => y.websiteFK == 1) &&
                        x.productStatusFK == 1)
                    .ToList();

                var equip = db.eqEquipment
                    .Include(x => x.eqCartridgeType)
                    .Include(x => x.manufacturer)
                    .OrderBy(x => x.eqEquipmentID).ToList();

                IndexWriter writerProducts = new IndexWriter(FSDirectory.Open(@"C:\Temp\Lucene\Product"), new
                    StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), true,
                    IndexWriter.MaxFieldLength.LIMITED);

                foreach (var prd in prods)
                {
                    Document document = new Document();
                    document.Add(new Field("ProductID", prd.productID.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                    document.Add(new Field("AxisID", prd.AxisFields.stockReference != null ? prd.AxisFields.stockReference : "", Field.Store.YES, Field.Index.NOT_ANALYZED));
                    document.Add(new Field("PartNo", prd.partNo, Field.Store.YES, Field.Index.ANALYZED));
                    document.Add(new Field("ProductName", prd.productName, Field.Store.YES, Field.Index.ANALYZED));
                    document.Add(new Field("ProductNameIndexed", prd.productName.Replace(" ", ""), Field.Store.YES, Field.Index.ANALYZED));
                    writerProducts.AddDocument(document);
                }

                writerProducts.Optimize();
                writerProducts.Dispose();

                IndexWriter writerEquipment = new IndexWriter(FSDirectory.Open(@"C:\Temp\Lucene\Equipment"), new
                    StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30), true,
                    IndexWriter.MaxFieldLength.LIMITED);

                foreach (var eq in equip)
                {
                    Document document = new Document();
                    document.Add(new Field("EquipID", eq.eqEquipmentID.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                    document.Add(new Field("EquipName", eq.description, Field.Store.YES, Field.Index.ANALYZED));
                    document.Add(new Field("EquipNameIndexed", eq.description.Replace(" ", ""), Field.Store.YES, Field.Index.ANALYZED));
                    document.Add(new Field("CartridgeTypeID", eq.eqCartridgeType.eqCartridgeTypeID.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                    document.Add(new Field("Manufacturer", eq.manufacturer.manufacturerName.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                    writerEquipment.AddDocument(document);
                }

                writerEquipment.Optimize();
                writerEquipment.Dispose();
            }

            return this;
        }

        public LuceneSearchViewModel SearchIndex()
        {
            DateTime startDate = DateTime.Now;

            string[] searchableFields = new string[] { "ProductName", "ProductNameIndexed", "PartNo" };

            var finalQuery = new BooleanQuery();
            var parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, searchableFields,
                new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));

            string[] terms = SearchTerm.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string term in terms)
            {
                finalQuery.Add(parser.Parse(term.Replace("~", "") + "~"), Occur.SHOULD);
                var wildQuery = new WildcardQuery(new Term("ProductNameIndexed", "*" + term + "*"));
                finalQuery.Add(wildQuery, Occur.SHOULD);
            }

            var directoryProduct = FSDirectory.Open(new DirectoryInfo(@"E:\PMS\Lucene\Product"));
            var searcherProduct = new IndexSearcher(directoryProduct, true);
            searcherProduct.SetDefaultFieldSortScoring(true, false);

            var hitsProduct = searcherProduct.Search(finalQuery, null, 30, Sort.RELEVANCE);

            foreach (ScoreDoc scoreDoc in hitsProduct.ScoreDocs)
            {
                Lucene.Net.Documents.Document doc = searcherProduct.Doc(scoreDoc.Doc);
                string part = doc.Get("PartNo");
                string text = doc.Get("ProductName");
                string type = doc.Get("Type");
                ProductResults.Add(type + " " + part + " " + text);
            }



            string[] searchableFieldsEquipment = new string[] { "EquipNameIndexed", "EquipName" };
            var finalQueryEquipment = new BooleanQuery();
            var parserEquipment = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, searchableFieldsEquipment, 
                new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30));

            foreach (string term in terms)
                finalQueryEquipment.Add(parserEquipment.Parse(term.Replace("~", "") + "~"), Occur.SHOULD);


            var directoryEquipment = FSDirectory.Open(new DirectoryInfo(@"E:\PMS\Lucene\Equipment"));
            var searcherEquipment = new IndexSearcher(directoryEquipment, true);
            searcherEquipment.SetDefaultFieldSortScoring(true, false);

            var hitsEquipment = searcherEquipment.Search(finalQueryEquipment, null, 30, Sort.RELEVANCE);

            foreach (ScoreDoc scoreDoc in hitsEquipment.ScoreDocs)
            {
                Lucene.Net.Documents.Document doc = searcherEquipment.Doc(scoreDoc.Doc);
                string equipName = doc.Get("EquipName");
                EquipResults.Add(equipName);
            }





            DateTime endDate = DateTime.Now;
            TimeSpan ts = endDate - startDate;

            TimeTaken = ts.TotalMilliseconds.ToString();

            return this;
        }
    }
}
