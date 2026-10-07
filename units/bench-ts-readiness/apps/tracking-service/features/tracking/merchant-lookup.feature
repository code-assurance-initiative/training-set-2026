Feature: Merchants look up their own parcels
  A merchant sees the parcels it registered and nothing about any other merchant's parcels, not even
  whether a tracking number exists.

  Background:
    Given merchant "merchant-a" has registered parcel "NPX12345678" with carrier "NPX"

  Scenario: The registering merchant sees the parcel
    When the carrier reports "OD" for parcel "NPX12345678" at "2026-10-05T07:10:00Z"
    And merchant "merchant-a" looks up parcel "NPX12345678"
    Then the merchant sees status "out_for_delivery"

  Scenario: Another merchant cannot tell the parcel exists
    When merchant "merchant-b" looks up parcel "NPX12345678"
    Then the lookup fails as not found
