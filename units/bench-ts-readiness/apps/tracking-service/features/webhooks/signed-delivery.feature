Feature: Merchants receive signed status changes
  Every status change is queued for the parcel's merchant and delivered to its webhook endpoint with a
  signature the merchant verifies with the published webhooks package. A rejected delivery is retried later.

  Background:
    Given merchant "merchant-a" has registered parcel "NPX12345678" with carrier "NPX"
    And merchant "merchant-a" receives webhooks at "https://merchant-a.example/hooks/parcels"

  Scenario: A delivery is reported to the merchant
    When the carrier reports "DL" for parcel "NPX12345678" at "2026-10-05T11:42:00Z"
    And the worker dispatches due webhooks
    Then the merchant has received 1 webhook for parcel "NPX12345678" with status "delivered"
    And every webhook carries a valid signature

  Scenario: A rejected delivery is retried after a minute
    Given the merchant's endpoint answers 503 once
    When the carrier reports "OD" for parcel "NPX12345678" at "2026-10-05T07:10:00Z"
    And the worker dispatches due webhooks
    Then the merchant has received 0 webhooks for parcel "NPX12345678" with status "out_for_delivery"
    When 1 minute passes
    And the worker dispatches due webhooks
    Then the merchant has received 1 webhook for parcel "NPX12345678" with status "out_for_delivery"
    And every webhook carries a valid signature
