<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:fn="http://www.w3.org/2005/02/xpath-functions" xmlns:xdt="http://www.w3.org/2005/02/xpath-datatypes" xmlns:date="http://exslt.org/dates-and-times" exclude-result-prefixes="xsl fn xs xdt date">
  <xsl:variable name="states" select="document('states.xml')"/>
  <xsl:variable name="countries" select="document('countries.xml')"/>

  <xsl:output method="xhtml" encoding="ISO-8859-1" indent="yes" omit-xml-declaration="yes" media-type="text/html" doctype-public="-//W3C//DTD XHTML 1.0 Transitional//EN" doctype-system="http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd"/>


  <!-- define the languages document for use -->
  <xsl:variable name="i18n" select="document('i18n.xml')" />
  <xsl:variable name="requiredlang" select="payment-model/language" as="xs:string"/>
  <xsl:variable name="lang">
    <xsl:choose>
      <xsl:when test="string-length($i18n/multi-lingual-text/texts[lang($requiredlang)]/text[@label='threed_d_page_tittle']) &gt; 0">
        <xsl:value-of select="payment-model/language" />
      </xsl:when>
      <xsl:otherwise>en</xsl:otherwise>
    </xsl:choose>
  </xsl:variable>
  <xsl:variable name="now" select="date:date-time()"/>
  <xsl:variable name="currentyear" select="date:year($now)"/>
  <xsl:variable name="currentmonth" select="date:month-in-year($now)"/>
  <xsl:variable name="imageloc" select="concat(payment-model/imagesURL,payment-model/vendor/vendortemplate,'/')" as="xs:string"/>

  <xsl:template name="replaceNewLine">
    <xsl:param name="string" />
    <xsl:choose>
      <xsl:when test="contains($string,'&#10;')">
        <xsl:value-of select="substring-before($string,'&#10;')" />
        <br/>
        <xsl:call-template name="replaceNewLine">
          <xsl:with-param name="string"
						select="substring-after($string,'&#10;')" />
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="$string" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="/">
    <xsl:variable name="vpsprotocol" select="payment-model/transaction/vpsprotocol"/>
    <xsl:variable name="cardtype" select="payment-model/paymentsystem/paymentsystemname"/>
    <xsl:variable name="transactiontypeid" select="payment-model/transaction/transactiontypeid"/>
    <xsl:variable name="usingtoken">
      <xsl:value-of select="payment-model/transaction/usingtoken"/>
    </xsl:variable>


    <html>
      <head>
        <title>
          <xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='sagepay_page_title']"/>
        </title>
        <style type="text/css" media="screen">
          @import url("<xsl:copy-of select="$imageloc"/>vsp3.css");
        </style>
        <SCRIPT type="text/javascript" language="javascript">
          <!--
	  //Preload images for this page-->
          if (document.images) {
          var cancel_btn = new Image()
          cancel_btn.src = "<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='cancel_img_file']"/>"
          var cancel_over = new Image()
          cancel_over.src = "<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='cancel_over_img_file']"/>"
          var proceed_btn = new Image()
          proceed_btn.src = "<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='paynow_img_file']"/>"
          var proceed_over = new Image()
          proceed_over.src = "<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='paynow_over_img_file']"/>"
          var submittedimg = new Image()
          submittedimg.src = "<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='submitted_img_file']"/>"
          var subsmallimg = new Image()
          subsmallimg.src = "<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='submitted_small_img_file']"/>"
          }
        </SCRIPT>
        <SCRIPT type="text/javascript" language="javascript" src="{concat(payment-model/imagesURL,'sagepay/buttonscript.js')}" />

        <SCRIPT type="text/javascript">
          function showHide(obj){
          var curSel=obj.options[obj.selectedIndex].value
          var show=(document.all)? 'block' : 'table-row';
          if(curSel=='US'){
          document.getElementById('stateRow').style.display=show;
          }else{
          document.getElementById('stateRow').style.display="none";
          }
          }
        </SCRIPT>
      </head>

      <body>
        <xsl:if test="$usingtoken != 'true' and $transactiontypeid != 15">
          <xsl:attribute name="onload">
            <xsl:text disable-output-escaping="yes">showHide(document.getElementById('cardcountry'))</xsl:text>
          </xsl:attribute>
        </xsl:if>
        <div id="pageWrapperLow">
          <div id="formCardDetails">
            <div style="text-align: center; padding: 5px;">
              <span class="errortext">
                <xsl:for-each select="//errormessage">
                  <xsl:value-of select="value"/>
                  <br/>
                </xsl:for-each>
              </span>
            </div>
            <form method="post" name="carddetails" id="carddetails">
              <xsl:attribute name="action">
                <xsl:value-of select="payment-model/carddetails-action-url"/>
              </xsl:attribute>
              <table class="formTable">
                <tr>
                  <td>
                    <span class="g-fw-b">CARD NUMBER</span>
                    <span>*</span>
                    <div>
                      <input name="cardnumber" size="30" autocomplete="off" placeholder="Card Number" />
                    </div>
                  </td>
                </tr>
                <tr>
                  <td>
                    <span class="g-fw-b">
                      FIRST NAME<span>*</span>
                    </span>
                    <div>
                      <input name="cardfirstnames" size="30" maxlength="20" autocomplete="off" placeholder="First Name" style="margin-right: 20px;" />
                    </div>
                  </td>
                  <td>
                    <span class="g-fw-b">
                      SURNAME<span>*</span>
                    </span>
                    <div class="name-container2">
                      <input name="cardsurname" size="30" maxlength="20" autocomplete="off" placeholder="Last Name" />
                    </div>
                  </td>
                </tr>
                <tr>
                  <td>
                    <span>VALID FROM</span>
                    <div>
                      <select name="startmonth" size="1" style="width: 80px;" class="g-m-r-30">
                        <option value="" selected="selected" disabled="disabled">Month</option>
                        <xsl:call-template name="generate_months">
                          <xsl:with-param name="start" select="1"/>
                          <xsl:with-param name="end" select="13"/>
                        </xsl:call-template>
                      </select>
                      <select name="startyear" size="1" class="expiryYearSelect" style="width: 80px; margin-left: 2px;">
                        <option value="" selected="selected" disabled="disabled">Year</option>
                        <xsl:call-template name="generate_years">
                          <xsl:with-param name="start" select="$currentyear - 20"/>
                          <xsl:with-param name="end" select="$currentyear + 1"/>
                        </xsl:call-template>
                      </select>
                    </div>
                  </td>
                </tr>
                <tr>
                  <td>
                    <span>EXPIRES</span>
                    <div>
                      <select name="expirymonth" size="1" style="width: 80px;" class="g-m-r-30">
                        <option value="" selected="selected" disabled="disabled">Month</option>
                        <xsl:call-template name="generate_months">
                          <xsl:with-param name="start" select="1"/>
                          <xsl:with-param name="end" select="13"/>
                        </xsl:call-template>
                      </select>
                      <select name="expiryyear" size="1" class="expiryYearSelect" style="width: 80px; margin-left: 2px;">
                        <option value="" selected="selected" disabled="disabled">Year</option>
                        <xsl:call-template name="generate_years">
                          <xsl:with-param name="start" select="$currentyear"/>
                          <xsl:with-param name="end" select="$currentyear + 20"/>
                        </xsl:call-template>
                      </select>
                    </div>
                  </td>
                </tr>
                <tr>
                  <td>
                    <span class="g-fw-b">ISSUE NUMBER (If Applicable)</span>
                    <span>*</span>
                    <div>
                      <input name="cardissue" size="2" maxlength="2" style="width: 200px" autocomplete="off" placeholder="Issue Number" />
                    </div>
                  </td>
                </tr>
                <tr>
                  <td>
                    <span class="g-fw-b">SECURITY CODE</span>
                    <span>*</span>
                    <div>
                      <input name="securitycode" size="5" maxlength="4" style="width: 100px" autocomplete="off" placeholder="CVV / CVC" />
                    </div>
                  </td>
                </tr>
              </table>
              <input name="action" type="hidden" value="proceed"/>
              <input type="hidden" name="clickedButton" value=""/>
              <a id="proceedButton" href="#" onclick="submitTheForm('carddetails','proceed');return false">Place Order</a>
              <!--a id="proceedButton" href="#" onmouseover="activate('proceed')" onmouseout="inactivate('proceed')" onclick="submitTheForm('carddetails','proceed');return false">
                    <SCRIPT type="text/javascript" language="JavaScript1.2">
                        document.write('<xsl:text disable-output-escaping="yes">&lt;img border="0" name="proceed" src="</xsl:text>');
                        document.write('<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='paynow_img_file']"/>');
                        document.write('<xsl:text disable-output-escaping="yes">" alt="</xsl:text>');
                        document.write('<xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='paynow']"/>');
                        document.write('<xsl:text disable-output-escaping="yes">"/&gt;</xsl:text>');
                    </SCRIPT>
                </a>
                <NOSCRIPT>
                    <input id="proceedButton" name="proceed" type="image">
                    <xsl:attribute name="src"><xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='paynow_img_file']"/></xsl:attribute>
                    <xsl:attribute name="alt"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='paynow']"/></xsl:attribute>
                    </input>
                </NOSCRIPT-->
            </form>
            <form method="post" name="cancelpayment" id="cancelpayment" action="/gateway/service/cancellation">
              <input type="hidden" name="clickedButton" value="" />
            </form>
          </div>
          <xsl:if test="payment-model/surcharge-info[node()]">
            <br/>
            <div class="surcharge">
              <xsl:call-template name="replaceNewLine">
                <xsl:with-param name="string"
                select="payment-model/surcharge-info" />
              </xsl:call-template>
            </div>
          </xsl:if>
        </div>
        <SCRIPT type="text/javascript" language="JavaScript">
          populate3dsFields();
        </SCRIPT>
      </body>
    </html>
  </xsl:template>
  <xsl:template name="generate_years">
    <xsl:param name="start" select="2007"/>
    <xsl:param name="end" select="2020"/>
    <xsl:if test="$start &lt; $end">
      <option>
        <xsl:attribute name="value">
          <xsl:value-of select="substring( string($start),3,2)"/>
        </xsl:attribute>
        <xsl:value-of select="$start"/>
      </option>
      <xsl:call-template name="generate_years">
        <xsl:with-param name="start" select="$start + 1"/>
        <xsl:with-param name="end" select="$end"/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

  <xsl:template name="generate_months">
    <xsl:param name="start" select="1"/>
    <xsl:param name="end" select="13"/>
    <xsl:param name="current" select="0"/>
    <xsl:if test="$start &lt; $end">
      <option>
        <xsl:choose>
          <xsl:when test="$start  &lt; 10">
            <xsl:attribute name="value">
              <xsl:value-of select="concat('0',string($start))"/>
            </xsl:attribute>
            <xsl:if test="$start = $current">
              <xsl:attribute name="selected">
                <xsl:value-of select="true"/>
              </xsl:attribute>
            </xsl:if>
            <xsl:value-of select="concat('0',string($start))"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:attribute name="value">
              <xsl:value-of select="string($start)"/>
            </xsl:attribute>
            <xsl:if test="$start = $current">
              <xsl:attribute name="selected">
                <xsl:value-of select="true"/>
              </xsl:attribute>
            </xsl:if>
            <xsl:value-of select="$start"/>
          </xsl:otherwise>
        </xsl:choose>
      </option>
      <xsl:call-template name="generate_months">
        <xsl:with-param name="start" select="$start + 1"/>
        <xsl:with-param name="end" select="$end"/>
        <xsl:with-param name="current" select="$current"/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

  <xsl:template name="popup_info">
    <xsl:param name="filename" select="test"/>
    <xsl:param name="i18n" select="document('i18n.xml')"/>
    <xsl:param name="imageloc" select="none"/>
    <xsl:param name="lang" select="en"/>
    <SCRIPT>
      document.write('<xsl:text disable-output-escaping="yes">&lt;a class="bodybold" href="javascript:popUp(\'</xsl:text>');
      document.write('<xsl:copy-of select="$imageloc"/><xsl:value-of select="$filename"/>');
      document.write('<xsl:text disable-output-escaping="yes">\');"&gt;&lt;img border="0" src="</xsl:text>');
      document.write('<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='whattodo_img']"/>');
      document.write('<xsl:text disable-output-escaping="yes">"/&gt;&lt;/a&gt;</xsl:text>');
    </SCRIPT>
    <NOSCRIPT>
      <a class="bodybold">
        <xsl:attribute name="href">
          <xsl:copy-of select="$imageloc"/>
          <xsl:value-of select="$filename"/>
        </xsl:attribute>
        <xsl:attribute name="target">_blank</xsl:attribute>
        <img border="0">
          <xsl:attribute name="src">
            <xsl:copy-of select="$imageloc"/>
            <xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='whattodo_img']"/>
          </xsl:attribute>
        </img>
      </a>
    </NOSCRIPT>
  </xsl:template>

  <xsl:template name="liststates">
    <xsl:param name="states" select="document('states.xml')"/>
    <xsl:param name="selection" select="AL"/>
    <xsl:for-each select="$states/states/state">
      <option>
        <xsl:attribute name="value">
          <xsl:value-of select="code"/>
        </xsl:attribute>
        <xsl:if test='$selection=code'>
          <xsl:attribute name="selected">yes</xsl:attribute>
        </xsl:if>
        <xsl:value-of select="name"/>
      </option>
    </xsl:for-each>
  </xsl:template>

  <xsl:template name="listcountries">
    <xsl:param name="countries" select="document('countries.xml')"/>
    <xsl:param name="selection" select="GB"/>

    <xsl:for-each select="$countries/countries/country">
      <option>
        <xsl:attribute name="value">
          <xsl:value-of select="code"/>
        </xsl:attribute>
        <xsl:if test='$selection=code'>
          <xsl:attribute name="selected">yes</xsl:attribute>
        </xsl:if>
        <xsl:value-of select="name"/>
      </option>
    </xsl:for-each>
  </xsl:template>
</xsl:stylesheet>
