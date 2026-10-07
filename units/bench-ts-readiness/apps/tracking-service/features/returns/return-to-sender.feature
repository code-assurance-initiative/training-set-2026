Feature: Parcels returned to the sender
  When a carrier gives up on a delivery and sends the parcel back, the merchant is told and can book the
  parcel out again under a new tracking number.

  Background:
    Given merchant "merchant-a" has registered parcel "NPX12345678" with carrier "NPX"

  Scenario: A returned parcel is final and the merchant is told
    When the carrier reports "RT" for parcel "NPX12345678" at "2026-10-09T15:00:00Z"
    Then parcel "NPX12345678" has status "returned"
    And the worker stops polling parcel "NPX12345678"
    And merchant "merchant-a" is notified that parcel "NPX12345678" is on its way back

  Scenario: A returned parcel is booked out again
    Given parcel "NPX12345678" has been returned to the sender
    When merchant "merchant-a" books parcel "NPX12345678" out again as "NPX99990001"
    Then parcel "NPX99990001" has status "created"
    And parcel "NPX12345678" refers to its replacement "NPX99990001"
