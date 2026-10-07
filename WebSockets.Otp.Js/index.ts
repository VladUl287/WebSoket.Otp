import { useWsClient } from "./client"

main()

async function main() {
    const client = useWsClient({
        url: "ws://localhost:5096/ws"
    })

    await client.connect()

    const result = await client.send<any, { message: string }>(
        'chat/message/send',
        {
            "chatId": "d5f30dff-b9a7-4292-96e0-61c84e5227ce",
            "content": "test",
            "timestamp": "2026-09-16T14:30:00.1234567+03:00"
        })

    const off = client.receive<{ message: string }>('test', (v) => {
        console.log(v)
        off()
    })

    console.log(result)

    client.close()
}