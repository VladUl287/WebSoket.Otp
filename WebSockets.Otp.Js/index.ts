import { useWsClient } from "./src/client"

main()

async function main() {
    const client = useWsClient({
        url: "ws://localhost:5096/ws"
    })

    await client.connect()

    const off = client.receive<{ message: string }>('notify/message/receive', (v) => {
        console.log(v)
        off()
    })

    const result = await client.send<any, { message: string }>(
        'chat/message/send',
        {
            "chatId": "d5f30dff-b9a7-4292-96e0-61c84e5227ce",
            "content": "test",
            "timestamp": "2026-09-16T14:30:00.1234567+03:00"
        })

    console.log(result)

    client.close()
}