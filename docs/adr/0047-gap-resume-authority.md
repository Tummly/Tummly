# Gap resume authority

When a create or recovery **Gap turn** is open (`DraftInterviewJson` holds a known gap), that open gap owns whether the next send continues the fill, cancels, keeps the gap while answering Retrieve / Refuse, or drops for a true replace create. Phrase classifiers must not clear the gap on ordinary fills such as Offer end-date answers.

The model may extract structured Offer terms into an open Offer-terms gap (Overlay after deterministic Merge makes no progress). The server still owns Gap ask copy (see ADR 0030), persist, cancel, and refuse. The server must not upgrade Retrieve to Create.

We rejected keeping top-level `LooksLikeNewCreateDuringGap` as the early drop gate: needles such as make + offer falsely drop mid-thread fills. We rejected a model round-trip for every bind Gap (Email vs SMS); only Offer-terms use an optional extract Overlay.

Surfaces covered by one Decide module: Offer path, Create Campaign with Offer, Create Campaign Draft, Recovery path, and create-target chips.
