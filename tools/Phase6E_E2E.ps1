$ErrorActionPreference = "Stop"
$base = "http://127.0.0.1:5088"
$log = @()
function Log($m) { $log += $m; Write-Host $m }
function J($o) { if ($null -eq $o) { return "null" }; return ($o | ConvertTo-Json -Depth 12 -Compress) }
function Call($method, $path, $body=$null, $token=$null) {
  $hdr = @{ Accept="application/json" }
  if ($token) { $hdr["Authorization"] = "Bearer $token" }
  $p = @{ Uri="$base$path"; Method=$method; Headers=$hdr; TimeoutSec=90 }
  if ($null -ne $body) { $p.ContentType="application/json"; $p.Body=($body | ConvertTo-Json -Depth 10 -Compress) }
  try { return Invoke-RestMethod @p }
  catch {
    $detail = $_.ErrorDetails.Message
    if (-not $detail -and $_.Exception.Response) {
      $sr = [IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
      $detail = $sr.ReadToEnd()
    }
    throw "$method $path :: $($_.Exception.Message) :: $detail"
  }
}

# --- Admin ---
$otp = Call POST "/api/v1/admin/auth/send-otp" @{ phone = "+201000000000" }
$code = $otp.data.debugCode
$adminLogin = Call POST "/api/v1/admin/auth/login" @{ phone = "+201000000000"; code = "$code" }
$adminTok = $adminLogin.data.accessToken
Log "ADMIN_OK"

$rideFares = Call GET "/api/v1/admin/ride-fare-rules" $null $adminTok
$groupFares = Call GET "/api/v1/admin/group-fare-rules" $null $adminTok
if (-not $rideFares.data -or @($rideFares.data).Count -eq 0) {
  $rf = Call POST "/api/v1/admin/ride-fare-rules" @{
    name = "Phase6E Ride flat (CarShuttle Cairo OneWayPrice)"
    flatFare = 90.00
    isActive = $true
  } $adminTok
  Log "RIDE_FARE_CREATED id=$($rf.data.id) flatFare=$($rf.data.flatFare)"
} else { Log "RIDE_FARE_REUSED count=$(@($rideFares.data).Count)" }

if (-not $groupFares.data -or @($groupFares.data).Count -eq 0) {
  $gf = Call POST "/api/v1/admin/group-fare-rules" @{
    name = "Phase6E Group charter (4 x CarShuttle Cairo OneWayPrice)"
    charterFlatFare = 360.00
    maxMembers = 4
    isActive = $true
  } $adminTok
  Log "GROUP_FARE_CREATED id=$($gf.data.id) charter=$($gf.data.charterFlatFare) max=$($gf.data.maxMembers)"
} else { Log "GROUP_FARE_REUSED count=$(@($groupFares.data).Count)" }

# --- Rider ---
$otpR = Call POST "/api/v1/auth/send-otp" @{ phone = "+201144340030"; purpose = "login" }
$authR = Call POST "/api/v1/auth/verify-otp" @{ phone = "+201144340030"; code = "$($otpR.data.debugCode)"; purpose = "login" }
$riderTok = $authR.data.tokens.accessToken
Log "RIDER_OK"

$quote = Invoke-RestMethod -Uri "$base/api/v1/rides/quote" -Headers @{ Authorization="Bearer $riderTok" }
Log "RIDE_QUOTE total=$($quote.data.totalAmount) fare=$($quote.data.fareAmount) paymentHint=$($quote.data.paymentMethodHint)"

$ride = Call POST "/api/v1/rides" @{
  pickupLatitude = 30.0444
  pickupLongitude = 31.2357
  destinationLatitude = 30.0626
  destinationLongitude = 31.2497
  pickupAddress = "Phase6E Ride pickup"
  destinationAddress = "Phase6E Ride dest"
  paymentMethod = "cash"
} $riderTok
$rideId = $ride.data.id
Log "RIDE_CREATED id=$rideId status=$($ride.data.status) total=$($ride.data.totalAmount) fare=$($ride.data.fareAmount) commission=$($ride.data.commissionAmount) captain=$($ride.data.captainEarnings) cash=$($ride.data.isCashConfirmed) payment=$($ride.data.paymentMethod) driver=$($ride.data.driverId)"

try {
  Call POST "/api/v1/rides" @{
    pickupLatitude=30.0; pickupLongitude=31.0; destinationLatitude=30.1; destinationLongitude=31.1
    paymentMethod="card"
  } $riderTok | Out-Null
  Log "RIDE_CARD_UNEXPECTED_PASS"
} catch { Log "RIDE_CARD_REJECTED_OK" }

$driverId = "419f36e6-61e7-42d2-807f-879557387891"
$asg = Call PUT "/api/v1/admin/rides/$rideId/driver" @{ driverId = $driverId } $adminTok
Log "RIDE_ASSIGNED status=$($asg.data.status) driverId=$($asg.data.driverId)"

# Captain
$otpC = Call POST "/api/v1/auth/send-otp" @{ phone = "+201008879097"; purpose = "login"; client = "driver" }
$authC = Call POST "/api/v1/auth/verify-otp" @{ phone = "+201008879097"; code = "$($otpC.data.debugCode)"; purpose = "login"; client = "driver" }
$capTok = $authC.data.tokens.accessToken
Log "CAPTAIN_OK"

$st = Call POST "/api/v1/drivers/me/rides/$rideId/start" @{} $capTok
Log "RIDE_START status=$($st.data.status) idem=$($st.data.idempotent)"
$st2 = Call POST "/api/v1/drivers/me/rides/$rideId/start" @{} $capTok
Log "RIDE_START_IDEM status=$($st2.data.status) idem=$($st2.data.idempotent)"
$cp = Call POST "/api/v1/drivers/me/rides/$rideId/complete" @{} $capTok
Log "RIDE_COMPLETE status=$($cp.data.status) idem=$($cp.data.idempotent)"
$cp2 = Call POST "/api/v1/drivers/me/rides/$rideId/complete" @{} $capTok
Log "RIDE_COMPLETE_IDEM status=$($cp2.data.status) idem=$($cp2.data.idempotent)"

$final = Call GET "/api/v1/rides/$rideId" $null $riderTok
Log "RIDE_FINAL status=$($final.data.status) total=$($final.data.totalAmount)"

# Wrong captain
$otpW = Call POST "/api/v1/auth/send-otp" @{ phone = "+201066969694"; purpose = "login"; client = "driver" }
$authW = Call POST "/api/v1/auth/verify-otp" @{ phone = "+201066969694"; code = "$($otpW.data.debugCode)"; purpose = "login"; client = "driver" }
$wrongTok = $authW.data.tokens.accessToken
$ride2 = Call POST "/api/v1/rides" @{
  pickupLatitude=30.05; pickupLongitude=31.24; destinationLatitude=30.06; destinationLongitude=31.25
  paymentMethod="cash"
} $riderTok
$ride2Id = $ride2.data.id
Call PUT "/api/v1/admin/rides/$ride2Id/driver" @{ driverId = $driverId } $adminTok | Out-Null
try {
  Call POST "/api/v1/drivers/me/rides/$ride2Id/start" @{} $wrongTok | Out-Null
  Log "WRONG_CAPTAIN_UNEXPECTED_PASS"
} catch { Log "WRONG_CAPTAIN_BLOCKED_OK" }
# cleanup cancel ride2 by admin path - or leave Assigned; cancel as rider before start if possible
# ride2 is Assigned - rider cancel may work
try {
  $cn = Call POST "/api/v1/rides/$ride2Id/cancel" @{} $riderTok
  Log "RIDE2_CANCELLED status=$($cn.data.status)"
} catch { Log "RIDE2_CANCEL_SKIP $($_.Exception.Message.Substring(0,80))" }

# --- Group E2E ---
$gCreate = Call POST "/api/v1/groups" @{
  pickupLatitude=30.0444; pickupLongitude=31.2357
  destinationLatitude=30.0626; destinationLongitude=31.2497
  pickupAddress="Phase6E Group pickup"; destinationAddress="Phase6E Group dest"
  capacity=2
} $riderTok
$groupId = $gCreate.data.id
Log "GROUP_CREATED id=$groupId status=$($gCreate.data.status) capacity=$($gCreate.data.capacity) joined=$($gCreate.data.joinedMemberCount) locked=$($gCreate.data.membershipLocked) total=$($gCreate.data.totalAmount)"

# Second member
$otpM = Call POST "/api/v1/auth/send-otp" @{ phone = "+201012345678"; purpose = "login" }
$authM = Call POST "/api/v1/auth/verify-otp" @{ phone = "+201012345678"; code = "$($otpM.data.debugCode)"; purpose = "login" }
$memTok = $authM.data.tokens.accessToken
$joined = Call POST "/api/v1/groups/$groupId/join" @{} $memTok
Log "GROUP_JOINED joined=$($joined.data.joinedMemberCount)"

# Capacity exceeded with 3rd? capacity=2, already 2 - try passenger 3
$otpM2 = Call POST "/api/v1/auth/send-otp" @{ phone = "+201024139021"; purpose = "login" }
$authM2 = Call POST "/api/v1/auth/verify-otp" @{ phone = "+201024139021"; code = "$($otpM2.data.debugCode)"; purpose = "login" }
$mem2Tok = $authM2.data.tokens.accessToken
try {
  Call POST "/api/v1/groups/$groupId/join" @{} $mem2Tok | Out-Null
  Log "GROUP_OVERCAP_UNEXPECTED_PASS"
} catch { Log "GROUP_CAPACITY_BLOCKED_OK" }

$confirmed = Call POST "/api/v1/groups/$groupId/confirm" @{ paymentMethod = "cash" } $riderTok
Log "GROUP_CONFIRMED status=$($confirmed.data.status) locked=$($confirmed.data.membershipLocked) cash=$($confirmed.data.isCashConfirmed) total=$($confirmed.data.totalAmount) payment=$($confirmed.data.paymentMethod) fare=$($confirmed.data.fareAmount) commission=$($confirmed.data.commissionAmount)"

try {
  Call POST "/api/v1/groups/$groupId/join" @{} $mem2Tok | Out-Null
  Log "GROUP_JOIN_AFTER_LOCK_UNEXPECTED"
} catch { Log "GROUP_LOCK_BLOCKS_JOIN_OK" }

$gasg = Call PUT "/api/v1/admin/groups/$groupId/driver" @{ driverId = $driverId } $adminTok
Log "GROUP_ASSIGNED status=$($gasg.data.status) driver=$($gasg.data.driverId)"

$gst = Call POST "/api/v1/drivers/me/groups/$groupId/start" @{} $capTok
Log "GROUP_START status=$($gst.data.status)"
$gcp = Call POST "/api/v1/drivers/me/groups/$groupId/complete" @{} $capTok
Log "GROUP_COMPLETE status=$($gcp.data.status)"
$gfinal = Call GET "/api/v1/groups/$groupId" $null $riderTok
Log "GROUP_FINAL status=$($gfinal.data.status) total=$($gfinal.data.totalAmount)"

# Snapshot immutability: change fare rule then re-read completed ride
Call POST "/api/v1/admin/ride-fare-rules" @{
  name = "Phase6E Ride flat UPDATED after E2E"
  flatFare = 99.00
  isActive = $true
} $adminTok | Out-Null
$after = Call GET "/api/v1/rides/$rideId" $null $riderTok
Log "SNAPSHOT_IMMUTABLE ride_total_still=$($after.data.totalAmount) (expect 90)"

$log | Set-Content "F:\aa_MOC\Flutter_Projects\shuttlez-cursor-api\docs\PHASE6E_E2E_RUN_LOG.txt"
Log "ALL_DONE"
