<?xml version="1.0" encoding="ISO-8859-1"?>

<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:fn="http://www.w3.org/2005/02/xpath-functions" xmlns:xdt="http://www.w3.org/2005/02/xpath-datatypes" xmlns:date="http://exslt.org/dates-and-times" exclude-result-prefixes="xsl fn xs xdt date">
  <xsl:variable name="countries" select="document('countries.xml')"/>
  <xsl:variable name="states" select="document('states.xml')"/>

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
    
    <xsl:template match="/">
    <xsl:variable name="vpsprotocol" select="payment-model/transaction/vpsprotocol"/>
		<xsl:variable name="cardtype" select="payment-model/paymentsystem/paymentsystemname"/>
		<xsl:variable name="transactiontypeid" select="payment-model/transaction/transactiontypeid"/>
		<xsl:variable name="usingtoken">
			<xsl:value-of select="payment-model/transaction/usingtoken"/>
		</xsl:variable>
		
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
	<title><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='sagepay_page_title']"/></title>
    <script>
      function detectMobile(){
        nv_type_pattern = /android|avantgo|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino|1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|comp(?!atible)|cond|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|e\-|e\/|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(di(?!a)|rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pluc|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|topl|toshiba|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|xda(\-|2|g)|yas\-|your|zeto|zte\-/i;
        var mobileBrow;
        if(nv_type_pattern.test(navigator.userAgent)){ 
          mobileBrow = true; 
        }
        else{ 
             mobileBrow = false; 
        }
        return mobileBrow; 
     }
      
      var ss = document.createElement("link");
      ss.type = "text/css";
      ss.rel = "stylesheet";
      if(detectMobile()){
           ss.href = "<xsl:copy-of select="$imageloc"/>mobile.css";
          
      }
      else{
        ss.href = "<xsl:copy-of select="$imageloc"/>vsp3.css";
      }
      document.getElementsByTagName("head")[0].appendChild(ss);
</script>

    <script type="text/javascript" language="javascript">
	  <!--
	  //Preload images for this page-->
	  if (document.images) {
		 var cancel_btn = new Image()
		 cancel_btn.src = "<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='cancel_img_file']"/>"
		 var cancel_over = new Image()
		 cancel_over.src = "<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='cancel_over_img_file']"/>"
		 var proceed_btn = new Image()
		 proceed_btn.src = "<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='proceed_img_file']"/>"
		 var proceed_over = new Image()
		 proceed_over.src = "<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='proceed_over_img_file']"/>"
		 var submittedimg = new Image()
		 submittedimg.src = "<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='submitted_img_file']"/>"
		 var subsmallimg = new Image()
		 subsmallimg.src = "<xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='submitted_small_img_file']"/>"
		 }
	</script>	 
    <script type="text/javascript" language="javascript" src="{concat(payment-model/imagesURL,'sagepay/buttonscript.js')}" />    

	<script type="text/javascript"> 
	function showHide(obj){
		var curSel=obj.options[obj.selectedIndex].value
		var show=(document.all)? 'block' : 'table-row';
		if(curSel=='US'){ 
			document.getElementById('stateRow').style.display=show;
		}else{
			document.getElementById('stateRow').style.display="none";
		} 
	} 
	</script>
</head>
<body>
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
                <xsl:attribute name="action"><xsl:value-of select="payment-model/carddetails-action-url"/></xsl:attribute>
                <table class="formTable">
                <xsl:if test="$usingtoken != 'true'">                    

                    <tr>
                        <td class="label"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='cardnumber']"/><span class="errortext"> *</span></td>
                        <td class="data"><input name="cardnumber" size="30" autocomplete="off" /></td>
                        <td class="info"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='enter_without_spaces']"/></td>
                    </tr>
                    <xsl:if test="$vpsprotocol &lt; 2.23">
                    <tr>
                        <td class="label"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='cardholdername']"/><span class="errortext"> *</span></td>
                        <td class="data"><input name="cardholder" size="30"><xsl:attribute name="value"><xsl:value-of select="payment-model/transaction/customername"/></xsl:attribute></input></td>
                        <td class="info"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='name_as_it_appears']"/></td>
                    </tr>
                    </xsl:if>
                    <xsl:if test="$vpsprotocol &gt; 2.22">
                    <tr>
                        <td class="label"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='cardfirstnames']"/><span class="errortext"> *</span></td>
                        <td class="data"><input name="cardfirstnames" size="30" maxlength="20" autocomplete="off" /></td>
                        <td class="info"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='name_as_it_appears']"/></td>
                    </tr>
                    <tr>
                        <td class="label"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='cardsurname']"/><span class="errortext"> *</span></td>
                        <td class="data"><input name="cardsurname" size="30" maxlength="20" autocomplete="off" /></td>
                        <td class="info" width="150"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='name_as_it_appears']"/></td>
                    </tr>								
                    </xsl:if>
                    <tr>
                        <td class="label"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='valid_from']"/></td>
                        <td class="data">
                            <span id="monthLabel"><span id="monthLabelText"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='month']"/></span></span>
                            <select name="startmonth" size="1" style="width: 51px; margin-left: 3px;">
                                <option value=""/>
                                <xsl:call-template name="generate_months">
                                    <xsl:with-param name="start" select="1"/>
                                    <xsl:with-param name="end" select="13"/>
                                </xsl:call-template>
                            </select>
                            <span style="padding-left: 10px;">
                                <span id="yearLabel"><span id="yearLabelText"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='year']"/></span></span>
                                <select name="startyear" size="1" class="expiryYearSelect" style="width: 65px; margin-left: 3px;">
                                    <option value=""/>
                                    <xsl:call-template name="generate_years">
                                        <xsl:with-param name="start" select="$currentyear - 20"/>
                                        <xsl:with-param name="end" select="$currentyear + 1"/>
                                    </xsl:call-template>
                                </select>
                            </span>
                        </td>
                        <td class="info"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='if_not_present_leave_blank']"/></td>
                    </tr>
                    <tr>
                        <td class="label"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='expiry_date']"/><span class="errortext"> *</span></td>
                        <td class="data" colspan="2">
                            <span id="monthLabel"><span id="monthLabelText"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='month']"/></span></span>
                            <select name="expirymonth" size="1" style="width: 51px; margin-left: 3px;">
								<option value=""/>
                                <xsl:call-template name="generate_months">
                                    <xsl:with-param name="start" select="1"/>
                                    <xsl:with-param name="end" select="13"/>
                                </xsl:call-template>
                            </select>
                            <span style="padding-left: 10px;">
                                <span id="yearLabel"><span id="yearLabelText"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='year']"/></span></span>
                                <select name="expiryyear" size="1" class="expiryYearSelect" style="width: 65px; margin-left: 3px;">
									<option value=""/>
                                    <xsl:call-template name="generate_years">
                                        <xsl:with-param name="start" select="$currentyear"/>
                                        <xsl:with-param name="end" select="$currentyear + 20"/>
                                    </xsl:call-template>
                                </select>
                            </span>
                        </td>
                    </tr>
                    <tr>
                        <td class="label">
                            <xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='issue_no']"/>
                        </td>
                        <td class="data">
                            <input size="2" maxlength="2" name="cardissue" style="width: 27px;" autocomplete="off" />
                        </td>
                        <td class="info">
                            <xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='issue_leave_it_blank']"/>
                        </td>
                    </tr>
										</xsl:if>
                    <tr>
                        <td class="label"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='security_code']"/><span class="errortext"> *</span></td>
                        <td class="data"><input size="5" maxlength="4" name="securitycode" style="width:50px;" autocomplete="off" /></td>
                        <td class="info">
						          		<xsl:if test="$cardtype='LASER'">
						          			<span class="info"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='laser_000_CV2']"/></span>
						          		</xsl:if>
                        </td>
                    </tr>
                    <xsl:variable name="allowgiftaid">
                    	<xsl:value-of select="payment-model/transaction/allowgiftaid"/>
                    </xsl:variable>
					<xsl:variable name="vendorSupportGiftaid">
						<xsl:value-of select="payment-model/vendor/giftaid"/>
					</xsl:variable>
															
										<xsl:if test="$vendorSupportGiftaid='true' and ($allowgiftaid='1' or $allowgiftaid='2')">
                    <tr>
                        <td class="label"><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='giftdetails']"/></td>
                        <td class="data" colspan="2">
                            <xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='gift_statement_part1']"/>
                            <strong><xsl:value-of select="payment-model/vendor/vendorprovidedname"/></strong><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='gift_statement_part2']"/><br /><br />
                            <xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='clickgift']"/>
                            <input name="giftaid" type="checkbox" value="yes">
                                <xsl:if test="$allowgiftaid='2'">
                                <xsl:attribute name="selected">true</xsl:attribute>
                                </xsl:if>
                            </input>
                        </td>
                    </tr>
                    </xsl:if>
                </table>
                <input name="action" type="hidden" value="proceed"/>
                <input type="hidden" name="clickedButton" value=""/>
                <a id="proceedButton" href="#" onclick="submitTheForm('carddetails','proceed');return false">
                    Place Order
                </a>
            </form>
            <form method="post" name="cancelpayment" id="cancelpayment">
            <xsl:attribute name="action"><xsl:value-of select="payment-model/cancelation-action-url"/></xsl:attribute>
                <input type="hidden" name="clickedButton" value=""/>
                <a id="cancelOrderButton" href="#" onclick="submitTheForm('cancelpayment','cancel');return false">
					Cancel
                </a>
            </form>                      
        </div>
    </div>
</body>
</html>
</xsl:template>
<xsl:template name="generate_years">
<xsl:param name="start" select="2007"/>
<xsl:param name="end" select="2020"/>
<xsl:if test="$start &lt; $end">
    <option>
        <xsl:attribute name="value"><xsl:value-of select="substring( string($start),3,2)"/></xsl:attribute>
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
                <xsl:attribute name="value"><xsl:value-of select="concat('0',string($start))"/></xsl:attribute>
                <xsl:if test="$start = $current">
                    <xsl:attribute name="selected"><xsl:value-of select="true"/></xsl:attribute>
                </xsl:if>
                <xsl:value-of select="concat('0',string($start))"/>
            </xsl:when>
            <xsl:otherwise>
                <xsl:attribute name="value"><xsl:value-of select="string($start)"/></xsl:attribute>
                <xsl:if test="$start = $current">
                    <xsl:attribute name="selected"><xsl:value-of select="true"/></xsl:attribute>
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
        <xsl:attribute name="href"><xsl:copy-of select="$imageloc"/><xsl:value-of select="$filename"/></xsl:attribute>
        <xsl:attribute name="target">_blank</xsl:attribute>
        <img border="0">
            <xsl:attribute name="src"><xsl:copy-of select="$imageloc"/><xsl:value-of select="$i18n/multi-lingual-text/texts[lang($lang)]/text[@label='whattodo_img']"/></xsl:attribute>
        </img>
    </a>
</NOSCRIPT>		
</xsl:template>

<xsl:template name="liststates">
    <xsl:param name="states" select="document('states.xml')"/>
    <xsl:param name="selection" select="AL"/>				
<xsl:for-each select="$states/states/state">		
    <option>
        <xsl:attribute name="value"><xsl:value-of select="code"/></xsl:attribute>
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
        <xsl:attribute name="value"><xsl:value-of select="code"/></xsl:attribute>
        <xsl:if test='$selection=code'>
            <xsl:attribute name="selected">yes</xsl:attribute>
        </xsl:if>
        <xsl:value-of select="name"/>
    </option>
</xsl:for-each>		
</xsl:template>
</xsl:stylesheet>
