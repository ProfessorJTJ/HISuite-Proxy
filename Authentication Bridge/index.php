<?php
$post = file_get_contents('php://input');
if(strpos($post, "\"2\"") !== false && strpos($post, "deviceCertificate") === false)
{
	$where = strpos($post, "deviceId");
	$where += 13;
	$finish = strpos($post, '"', $where) + 1;
	
	$appenddata = "STATIC DEVICE ID" . "\",\r\n"; // has to be changed
    $appenddata .= "   \"deviceCertificate\": \"" . "STATIC DEVICE CERTIFICATE" . "\",\r\n"; // has to be changed
    $appenddata .= "   \"keyAttestation\": \"" . "STATIC KEY ATTESTATION" . "\""; // has to be changed  
	$newrequestbody = substr($post, 0, $where) . $appenddata . substr($post, $finish);
	
	$ch = curl_init();
    curl_setopt($ch, CURLOPT_URL, "https://query.hicloud.com:443/sp_ard_common/v1/authorize.action");
    curl_setopt($ch, CURLOPT_HEADER, 0);
    curl_setopt($ch, CURLOPT_TIMEOUT, 20);
	curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, 0);
	curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, 0);
	curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
	curl_setopt($ch, CURLOPT_POST, 1);
	curl_setopt($ch, CURLOPT_POSTFIELDS, $newrequestbody);

	$resp = curl_exec($ch);
	curl_close($ch);
	if(strpos($resp, "data=") !== false)
	{
		header("Content-Type: text/plain;charset=UTF-8");
		echo $resp;
	}
}
?>