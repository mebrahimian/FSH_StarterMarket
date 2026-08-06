export type CodalLetterCategory = {
    value: string;
    group: number;
    labelKey: string;
    letCodes: readonly (number | null)[];
};

export const codalLetterCategoryOptions:
    readonly CodalLetterCategory[] = [
        {
            value: "materialInformation",
            group: 2,
            labelKey:
                "letterCategories.materialInformation",
            letCodes: [11, 174],
        },
        {
            value: "monthlyActivity",
            group: 3,
            labelKey:
                "letterCategories.monthlyActivity",
            letCodes: [6, 8, 58],
        },
        {
            value: "boardAndAuditCommittee",
            group: 5,
            labelKey:
                "letterCategories.boardAndAuditCommittee",
            letCodes: [19, 56, 60],
        },
        {
            value: "generalMeetings",
            group: 6,
            labelKey:
                "letterCategories.generalMeetings",
            letCodes: [
                16,
                17,
                18,
                20,
                21,
                22,
                26,
                52,
                55,
                120,
                121,
                122,
                154,
                2020,
                2121,
                2222,
            ],
        },
        {
            value: "capitalIncrease",
            group: 7,
            labelKey:
                "letterCategories.capitalIncrease",
            letCodes: [
                23,
                24,
                25,
                27,
                28,
                45,
                54,
                57,
                89,
                119,
            ],
        },
        {
            value: "clarifications",
            group: 8,
            labelKey:
                "letterCategories.clarifications",
            letCodes: [
                128,
                130,
                131,
                134,
                135,
                136,
                139,
                140,
                141,
                143,
                144,
                146,
                147,
                148,
                150,
                168,
                169,
                170,
            ],
        },
        {
            value: "other",
            group: 10,
            labelKey:
                "letterCategories.other",
            letCodes: [
                90,
                91,
                132,
                133,
                156,
                157,
                178,
                248,
                260,
                null,
            ],
        },
    ];